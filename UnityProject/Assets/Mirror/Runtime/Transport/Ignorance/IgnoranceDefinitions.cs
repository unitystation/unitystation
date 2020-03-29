using System;
using ENet;

namespace Mirror {
        // Snipped from the transport files, as this will help
        // me keep things up to date.
        [Serializable]
        public enum IgnoranceChannelTypes
        {
            Reliable = PacketFlags.Reliable,                                        // TCP Emulation.
            ReliableUnsequenced = PacketFlags.Reliable | PacketFlags.Unsequenced,   // TCP Emulation, but no sequencing.
            ReliableUnbundledInstant = PacketFlags.Reliable | PacketFlags.Instant,  // Experimental: Reliablity + Instant hybrid packet type.
            Unreliable = PacketFlags.Unsequenced,                                   // Pure UDP + ENet's Protocol.
            UnreliableFragmented = PacketFlags.UnreliableFragmented,                // Pure UDP, but fragmented.
            UnreliableSequenced = PacketFlags.None,                                 // Pure UDP, but sequenced.
            UnbundledInstant = PacketFlags.Instant,                                 // Instant packet, will not be bundled with others.
        }
}
