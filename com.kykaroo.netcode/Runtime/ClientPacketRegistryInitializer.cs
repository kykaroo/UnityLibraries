using System;
using System.Collections.Generic;

namespace NetcodePackage.Runtime
{
    public class ClientPacketRegistryInitializer
    {
        private readonly PacketRegistry _registry;
        private readonly List<(Type, Delegate)> _packetsActions;

        public ClientPacketRegistryInitializer(PacketRegistry registry, List<(Type, Delegate)> packetsActions)
        {
            _registry = registry;
            _packetsActions = packetsActions;
        }

        public void RegisterAll()
        {
            foreach (var (packetType, handler) in _packetsActions)
            {
                var id = GetPacketId(packetType);
                RegisterDynamic(id, packetType, handler);
            }
        }

        private void RegisterDynamic(ushort id, Type type, Delegate handler)
        {
            var method = typeof(PacketRegistry).GetMethod(nameof(PacketRegistry.Register))?.MakeGenericMethod(type);

            if (method != null) method.Invoke(_registry, new object[] { id, handler });
        }
        
        private ushort GetPacketId(Type packetType)
        {
            var packet = (INetworkPacket)Activator.CreateInstance(packetType);
            return packet.Id;
        }
    }
}