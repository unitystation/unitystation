using System;
using ENet;

namespace IgnoranceTransport
{
    // Snipped from the transport files, as this will help
    // me keep things up to date.
    [Serializable]
    public enum IgnoranceChannelTypes
    {
        Reliable = PacketFlags.Reliable,                                        // TCP Emulation.
        ReliableUnsequenced = PacketFlags.Reliable | PacketFlags.Unsequenced,   // TCP Emulation, but no sequencing.
        Unreliable = PacketFlags.Unsequenced,                                   // Pure UDP.
        UnreliableFragmented = PacketFlags.UnreliableFragmented,                // Pure UDP, but fragmented.
        UnreliableSequenced = PacketFlags.None,                                 // Pure UDP, but sequenced.
        Unthrottled = PacketFlags.Unthrottled,                                  // Apparently ENet's version of Taco Bell.
    }

    public class IgnoranceInternals
    {
        public const string Version = "1.4.0b6";
        public const string Scheme = "enet";
        public const string BindAllIPv4 = "0.0.0.0";
        public const string BindAllMacs = "::0";
    }

    public enum IgnoranceLogType
    {
        Nothing,
        Standard,
        Verbose
    }

    // Struct optimized for cache efficiency. (Thanks Vincenzo!)
    public struct IgnoranceIncomingPacket
    {
        public byte Channel;
        public uint NativePeerId;
        public Packet Payload;
    }

    // Struct optimized for cache efficiency. (Thanks Vincenzo!)
    public struct IgnoranceOutgoingPacket
    {
        public byte Channel;
        public uint NativePeerId;
        public Packet Payload;
    }

    // Struct optimized for cache efficiency. (Thanks Vincenzo!)
    public struct IgnoranceConnectionEvent
    {
        public bool WasDisconnect;
        public ushort Port;
        public uint NativePeerId;
        public string IP;
    }

    public struct IgnoranceCommandPacket
    {
        public IgnoranceCommandType Type;
        public uint PeerId;
    }

    public struct IgnoranceClientStats
    {
        // Stats only - may not always be used!
        public uint RTT;
        public ulong BytesReceived;
        public ulong BytesSent;
        public ulong PacketsReceived;
        public ulong PacketsSent;
        public ulong PacketsLost;
    }

    public enum IgnoranceCommandType
    {
        // Client
        ClientWantsToStop,
        ClientRequestsStatusUpdate,
        // ENet internal
        ResponseToClientStatusRequest,
        // Server
        ServerKickPeer
    }

    // TODO: Optimize struct for Cache performance.
    public struct PeerConnectionData
    {
        public ushort Port;
        public uint NativePeerId;       
        public string IP;
    }
}
