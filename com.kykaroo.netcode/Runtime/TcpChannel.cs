using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace com.kykaroo.netcode.Runtime
{
    public class TcpChannel
    {
        private readonly PacketRegistry _packetRegistry;
        private readonly TcpClient _client;
        private NetworkStream _stream;
        private bool _isListening;
        private readonly MemoryStream _sendBuffer = new(4096);
        private readonly object _sendLock = new();
        private readonly ConcurrentQueue<byte[]> _sendQueue = new();
        private readonly SemaphoreSlim _sendSignal = new(0);

        public TcpChannel(TcpClient client, PacketRegistry packetRegistry)
        {
            _client = client;
            _packetRegistry = packetRegistry;
        }

        public async Task SendImmediateAsync(INetworkPacket packet)
        {
            if (_isListening == false) return;

            try
            {
                using var ms = new MemoryStream();
                await using var writer = new BinaryWriter(ms);

                writer.Write(packet.Id);
                packet.Serialize(writer);

                var data = ms.ToArray();

                var lengthBytes = ArrayPool<byte>.Shared.Rent(2);

                try
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(lengthBytes, (ushort)data.Length);

                    var packetToSend = new byte[2 + data.Length];
                    Buffer.BlockCopy(lengthBytes, 0, packetToSend, 0, 2);
                    Buffer.BlockCopy(data, 0, packetToSend, 2, data.Length);

                    EnqueueTickBuffer(packetToSend);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(lengthBytes);
                }
            }
            catch (ObjectDisposedException)
            {
                Debug.LogError("[TCP] Stream closed, disconnecting...");
                HandleDisconnect();
            }
            catch (IOException ioEx)
            {
                Debug.LogError($"[TCP] IO error while sending: {ioEx.Message}");
                HandleDisconnect();
            }
            catch (SocketException sockEx)
            {
                Debug.LogError($"[TCP] Socket error while sending: {sockEx.SocketErrorCode}");
                HandleDisconnect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCP] Unexpected send error: {ex}");
                HandleDisconnect();
            }
        }

        public async Task EnqueueSendAsync(INetworkPacket packet)
        {
            if (_isListening == false) return;

            try
            {
                using var ms = new MemoryStream();
                await using var writer = new BinaryWriter(ms);

                writer.Write(packet.Id);
                packet.Serialize(writer);

                var data = ms.ToArray();

                var lengthBytes = ArrayPool<byte>.Shared.Rent(2);

                try
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(lengthBytes, (ushort)data.Length);

                    lock (_sendLock)
                    {
                        _sendBuffer.Write(lengthBytes, 0, 2);
                        _sendBuffer.Write(data, 0, data.Length);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(lengthBytes);
                }
            }
            catch (ObjectDisposedException)
            {
                Debug.LogError("[TCP] Stream closed, disconnecting...");
                HandleDisconnect();
            }
            catch (IOException ioEx)
            {
                Debug.LogError($"[TCP] IO error while sending: {ioEx.Message}");
                HandleDisconnect();
            }
            catch (SocketException sockEx)
            {
                Debug.LogError($"[TCP] Socket error while sending: {sockEx.SocketErrorCode}");
                HandleDisconnect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCP] Unexpected send error: {ex}");
                HandleDisconnect();
            }
        }

        private async Task SendLoopAsync()
        {
            while (_isListening)
            {
                await _sendSignal.WaitAsync();

                while (_sendQueue.TryDequeue(out var data))
                {
                    try
                    {
                        await _stream.WriteAsync(data, 0, data.Length);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[TCP] SendLoop error: {e}");
                        HandleDisconnect();

                        return;
                    }
                }

                await _stream.FlushAsync();
            }
        }

        private void HandleDisconnect()
        {
            _isListening = false;
        }

        public void StartListening(Action<INetworkPacket> onPacket)
        {
            _isListening = true;
            _stream = _client.GetStream();
            _ = Task.Run(() => _ = ListenLoop(onPacket));
            _ = Task.Run(() => _ = SendLoopAsync());
        }

        private async Task ListenLoop(Action<INetworkPacket> onPacket)
        {
            var buffer = new byte[4096];
            var leftover = new List<byte>();

            try
            {
                while (_isListening)
                {
                    var bytesRead = await _stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

                    if (bytesRead == 0)
                    {
                        Debug.LogError("[TCP] Connection closed by remote host.");
                        HandleDisconnect();

                        break;
                    }

                    leftover.AddRange(buffer[..bytesRead]);

                    while (leftover.Count >= 2)
                    {
                        var length = BitConverter.ToUInt16(leftover.ToArray(), 0);

                        if (leftover.Count < 2 + length)
                        {
                            break;
                        }

                        var packetData = leftover.GetRange(2, length).ToArray();

                        using var ms = new MemoryStream(packetData);
                        using var br = new BinaryReader(ms);

                        var id = br.ReadUInt16();
                        var packet = _packetRegistry.Create(id);
                        packet?.Deserialize(br);

                        if (packet != null)
                            onPacket?.Invoke(packet);

                        leftover.RemoveRange(0, 2 + length);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TCP] Read error: {e.Message}");

                HandleDisconnect();
            }
        }

        private async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            var totalRead = 0;

            while (totalRead < count)
            {
                var read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead);

                if (read == 0)
                {
                    return 0;
                }

                totalRead += read;
            }

            if (totalRead == 0)
            {
                Debug.LogError("error");
            }

            return totalRead;
        }

        public void Stop() => HandleDisconnect();

        public void Flush()
        {
            byte[] dataToSend;

            lock (_sendLock)
            {
                if (_sendBuffer.Length == 0)
                    return;

                dataToSend = _sendBuffer.ToArray();
                _sendBuffer.SetLength(0);
                _sendBuffer.Position = 0;
            }

            EnqueueTickBuffer(dataToSend);
        }

        private void EnqueueTickBuffer(byte[] buffer)
        {
            _sendQueue.Enqueue(buffer);
            _sendSignal.Release();
        }
    }
}