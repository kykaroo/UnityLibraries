using System;
using System.IO;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NetcodePackage.Runtime
{
    public class TcpChannel
    {
        private readonly PacketRegistry _packetRegistry;
        private readonly TcpClient _client;
        private NetworkStream _stream;
        private bool _isListening;

        public TcpChannel(TcpClient client, PacketRegistry packetRegistry)
        {
            _client = client;
            _packetRegistry = packetRegistry;
        }

        public async UniTaskVoid SendAsync(INetworkPacket packet)
        {
            if (_isListening == false) return;

            try
            {
                using var ms = new MemoryStream();
                await using var writer = new BinaryWriter(ms);

                writer.Write(packet.Id);
                packet.Serialize(writer);

                var data = ms.ToArray();

                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
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

        private void HandleDisconnect()
        {
            _isListening = false;
        }

        public void StartListening(Action<INetworkPacket> onPacket)
        {
            _isListening = true;
            _stream = _client.GetStream();
            UniTask.RunOnThreadPool(() => ListenLoop(onPacket));
        }

        private async UniTaskVoid ListenLoop(Action<INetworkPacket> onPacket)
        {
            var lengthBuffer = new byte[2];

            try
            {
                while (_isListening)
                {
                    var bytesRead = await ReadExactAsync(_stream, lengthBuffer, 0, 2);

                    if (bytesRead == 0)
                    {
                        Debug.LogError("[TCP] Connection closed by remote host.");
                        HandleDisconnect();

                        break;
                    }

                    var length = BitConverter.ToUInt16(lengthBuffer, 0);
                    var data = new byte[length];

                    bytesRead = await ReadExactAsync(_stream, data, 0, length);

                    if (bytesRead == 0)
                    {
                        Debug.LogError("[TCP] Connection closed during packet read.");

                        HandleDisconnect();

                        break;
                    }

                    using var ms = new MemoryStream(data);
                    using var br = new BinaryReader(ms);

                    var id = br.ReadUInt16();
                    var packet = _packetRegistry.Create(id);

                    packet?.Deserialize(br);
                    UniTask.Post(() => { onPacket?.Invoke(packet); });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TCP] Read error: {e.Message}");

                HandleDisconnect();
            }
        }

        private async UniTask<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count)
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

            return totalRead;
        }

        public void Stop() => HandleDisconnect();
    }
}