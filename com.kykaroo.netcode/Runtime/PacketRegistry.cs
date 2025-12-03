using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace NetcodePackage.Runtime
{
    public class PacketRegistry
    {
        private readonly Dictionary<ushort, Func<INetworkPacket>> _constructors = new();
        private readonly Dictionary<ushort, Action<INetworkPacket>> _handlers = new();

        public void Register<T>(ushort id, Action<T>? handler) where T : INetworkPacket, new()
        {
            _constructors[id] = () => new T();

            if (handler != null)
            {
                _handlers[id] = packet => handler((T)packet);
            }
        }

        [CanBeNull]
        public INetworkPacket Create(ushort id)
        {
            return _constructors.TryGetValue(id, out var ctor) ? ctor() : null;
        }

        public void Handle(INetworkPacket packet)
        {
            if (_handlers.TryGetValue(packet.Id, out var handler))
            {
                handler(packet);
            }
            else
            {
                Console.WriteLine($"No handler for packet id '{packet.Id}'");
            }
        }
    }
}