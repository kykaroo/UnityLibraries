using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace com.kykaroo.netcode.Runtime
{
    public class UdpChannel
    {
        private readonly PacketRegistry _packetRegistry;

        private readonly UdpClient _udp;
        private readonly IPEndPoint _remote;

        private bool _isListening;

        private readonly SynchronizationContext _unityContext = SynchronizationContext.Current;

        public UdpChannel(UdpClient udp, IPEndPoint remote, PacketRegistry packetRegistry)
        {
            _udp = udp;
            _remote = remote;
            _packetRegistry = packetRegistry;

            _isListening = true;
        }

        public async Task SendAsync(INetworkPacket packet)
        {
            try
            {
                var buffer = ArrayPool<byte>.Shared.Rent(1024);

                try
                {
                    using var ms = new MemoryStream();
                    await using var writer = new BinaryWriter(ms);

                    writer.Write(packet.Id);
                    packet.Serialize(writer);

                    await _udp.SendAsync(ms.GetBuffer(), (int)ms.Length, _remote);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            catch (ObjectDisposedException)
            {
                Debug.LogError("[UDP] Stream closed, disconnecting...");
                HandleDisconnect();
            }
            catch (IOException ioEx)
            {
                Debug.LogError($"[UDP] IO error while sending: {ioEx.Message}");
                HandleDisconnect();
            }
            catch (SocketException sockEx)
            {
                Debug.LogError($"[UDP] Socket error while sending: {sockEx.SocketErrorCode}");
                HandleDisconnect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UDP] Unexpected send error: {ex}");
                HandleDisconnect();
            }
        }

        private void HandleDisconnect()
        {
            _isListening = false;
        }

        public void StartListening(Action<IPEndPoint, INetworkPacket> onPacket)
        {
            _isListening = true;
            _ = ListenAsync(onPacket);
        }

        private async Task ListenAsync(Action<IPEndPoint, INetworkPacket> onPacket)
        {
            while (_isListening)
            {
                try
                {
                    var result = await _udp.ReceiveAsync();

                    _unityContext.Post(_ =>
                    {
                        using var ms = new MemoryStream(result.Buffer);
                        using var r = new BinaryReader(ms);

                        var id = r.ReadUInt16();
                        var packet = _packetRegistry.Create(id);

                        if (packet == null) return;

                        packet.Deserialize(r);
                        onPacket?.Invoke(_remote, packet);
                    }, null);
                }
                catch (ObjectDisposedException)
                {
                    Debug.LogError("[UDP] Socket closed.");

                    HandleDisconnect();

                    break;
                }
                catch (SocketException e)
                {
                    Debug.LogError($"[UDP] Socket error: {e.SocketErrorCode}");

                    HandleDisconnect();

                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UDP] Listen error: {e.Message}");
                    await Task.Yield();
                }
            }
        }

        public void Stop()
        {
            HandleDisconnect();

            try
            {
                _udp?.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UDP] Stop error: {e.Message}");
            }
        }
    }
}