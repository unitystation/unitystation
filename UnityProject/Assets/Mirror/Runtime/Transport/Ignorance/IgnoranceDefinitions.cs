// Ignorance 1.4.x LTS (Long Term Support)
// https://github.com/SoftwareGuy/Ignorance
// -----------------
// Copyright (c) 2019 - 2021 Matt Coburn (SoftwareGuy/Coburn64)
// Ignorance is licensed under the MIT license. Refer
// to the LICENSE file for more information.
using System;
using System.Collections.Generic;
using ENet;

namespace IgnoranceCore
{
    // Snipped from the transport files, as this will help
    // me keep things up to date.
    [Serializable]
    public enum IgnoranceChannelTypes
    {
        Reliable = PacketFlags.Reliable,                                        // Reliable UDP (TCP-like emulation)
        ReliableUnsequenced = PacketFlags.Reliable | PacketFlags.Unsequenced,   // Reliable UDP (TCP-like emulation w/o sequencing)
        Unreliable = PacketFlags.Unsequenced,                                   // Pure UDP, high velocity packet action.
        UnreliableFragmented = PacketFlags.UnreliableFragmented,                // Pure UDP, but fragmented.
        UnreliableSequenced = PacketFlags.None,                                 // Pure UDP, but sequenced.
        Unthrottled = PacketFlags.Unthrottled,                                  // Pure UDP. Literally turbo mode.
    }

    public class IgnoranceInternals
    {
        public const string Version = "1.4.0r1 (LTS)";
        public const string Scheme = "enet";
        public const string BindAnyAddress = "::0";
    }

    public enum IgnoranceLogType
    {
        Nothing,
        Standard,
        Verbose
    }

    public struct IgnoranceIncomingPacket
    {
        public byte Channel;
        public uint NativePeerId;
        public Packet Payload;
    }

    public struct IgnoranceOutgoingPacket
    {
        public byte Channel;
        public uint NativePeerId;
        public Packet Payload;
    }

    public struct IgnoranceConnectionEvent
    {
        public byte EventType;
        public ushort Port;
        public uint NativePeerId;
        public string IP;
    }

    public struct IgnoranceCommandPacket
    {
        public IgnoranceCommandType Type;
        public uint PeerId;
    }

    // Stats only - may not always be used!
    public struct IgnoranceClientStats
    {
        
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
        ClientStatusRequest,
        // Server
        ServerKickPeer,
        ServerStatusRequest
    }

    // Stats only - may not always be used!
    public struct IgnoranceServerStats
    {
        
        public ulong BytesReceived;
        public ulong BytesSent;
        public ulong PacketsReceived;
        public ulong PacketsSent;
        public ulong PeersCount;

        public Dictionary<int, IgnoranceClientStats> PeerStats;
    }

    public struct PeerConnectionData
    {        
        public ushort Port;
        public uint NativePeerId;
        public string IP;
    }
}
