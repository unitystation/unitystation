/*
 *  Managed C# wrapper for an extended version of ENet
 *  Copyright (c) 2019 Matt Coburn (SoftwareGuy/Coburn64), Chris Burns (c6burns)
 *  Copyright (c) 2013 - 2018 James Bellinger, Nate Shoffner, Stanislav Denisov
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace ENet
{
    [Flags]
    public enum PacketFlags
    {
        None = 0,
        Reliable = 1 << 0,
        Unsequenced = 1 << 1,
        NoAllocate = 1 << 2,
        UnreliableFragmented = 1 << 3,
        Instant = 1 << 4
    }

    public enum EventType
    {
        None = 0,
        Connect = 1,
        Disconnect = 2,
        Receive = 3,
        Timeout = 4
    }

    public enum PeerState
    {
        Uninitialized = -1,
        Disconnected = 0,
        Connecting = 1,
        AcknowledgingConnect = 2,
        ConnectionPending = 3,
        ConnectionSucceeded = 4,
        Connected = 5,
        DisconnectLater = 6,
        Disconnecting = 7,
        AcknowledgingDisconnect = 8,
        Zombie = 9
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ENetAddress
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] ip;
        public ushort port;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ENetEvent
    {
        public EventType type;
        public IntPtr peer;
        public byte channelID;
        public uint data;
        public IntPtr packet;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ENetCallbacks
    {
        public AllocCallback malloc;
        public FreeCallback free;
        public NoMemoryCallback noMemory;
    }

    public delegate IntPtr AllocCallback(IntPtr size);
    public delegate void FreeCallback(IntPtr memory);
    public delegate void NoMemoryCallback();
    public delegate void PacketFreeCallback(Packet packet);

    internal static class ArrayPool
    {
        [ThreadStatic]
        private static byte[] byteBuffer;
        [ThreadStatic]
        private static IntPtr[] pointerBuffer;

        public static byte[] GetByteBuffer()
        {
            if (byteBuffer == null)
                byteBuffer = new byte[64];

            return byteBuffer;
        }

        public static IntPtr[] GetPointerBuffer()
        {
            if (pointerBuffer == null)
                pointerBuffer = new IntPtr[Library.maxPeers];

            return pointerBuffer;
        }
    }

    public struct Address
    {
        private ENetAddress nativeAddress;

        internal ENetAddress NativeData {
            get {
                return nativeAddress;
            }

            set {
                nativeAddress = value;
            }
        }

        internal Address(ENetAddress address)
        {
            nativeAddress = address;
        }

        public ushort Port {
            get {
                return nativeAddress.port;
            }

            set {
                nativeAddress.port = value;
            }
        }

        public string GetIP()
        {
            StringBuilder ip = new StringBuilder(1024);

            if (Native.enet_address_get_host_ip(nativeAddress, ip, (IntPtr)ip.Capacity) != 0)
                return String.Empty;

            return ip.ToString();
        }

        public bool SetIP(string ip)
        {
            if (ip == null)
                throw new ArgumentNullException("ip");

            return Native.enet_address_set_host_ip(ref nativeAddress, ip) == 0;
        }

        public string GetHost()
        {
            StringBuilder hostName = new StringBuilder(1024);

            if (Native.enet_address_get_host(nativeAddress, hostName, (IntPtr)hostName.Capacity) != 0)
                return String.Empty;

            return hostName.ToString();
        }

        public bool SetHost(string hostName)
        {
            if (hostName == null)
                throw new ArgumentNullException("hostName");

            return Native.enet_address_set_host(ref nativeAddress, hostName) == 0;
        }
    }

    public struct Event
    {
        private ENetEvent nativeEvent;

        internal ENetEvent NativeData {
            get {
                return nativeEvent;
            }

            set {
                nativeEvent = value;
            }
        }

        internal Event(ENetEvent @event)
        {
            nativeEvent = @event;
        }

        public EventType Type {
            get {
                return nativeEvent.type;
            }
        }

        public Peer Peer {
            get {
                return new Peer(nativeEvent.peer);
            }
        }

        public byte ChannelID {
            get {
                return nativeEvent.channelID;
            }
        }

        public uint Data {
            get {
                return nativeEvent.data;
            }
        }

        public Packet Packet {
            get {
                return new Packet(nativeEvent.packet);
            }
        }
    }

    public class Callbacks
    {
        private ENetCallbacks nativeCallbacks;

        internal ENetCallbacks NativeData {
            get {
                return nativeCallbacks;
            }

            set {
                nativeCallbacks = value;
            }
        }

        public Callbacks(AllocCallback allocCallback, FreeCallback freeCallback, NoMemoryCallback noMemoryCallback)
        {
            nativeCallbacks.malloc = allocCallback;
            nativeCallbacks.free = freeCallback;
            nativeCallbacks.noMemory = noMemoryCallback;
        }
    }

    public struct Packet : IDisposable
    {
        private IntPtr nativePacket;

        internal IntPtr NativeData {
            get {
                return nativePacket;
            }

            set {
                nativePacket = value;
            }
        }

        internal Packet(IntPtr packet)
        {
            nativePacket = packet;
        }

        public void Dispose()
        {
            if (nativePacket != IntPtr.Zero)
            {
                Native.enet_packet_dispose(nativePacket);
                nativePacket = IntPtr.Zero;
            }
        }

        public bool IsSet {
            get {
                return nativePacket != IntPtr.Zero;
            }
        }

        public IntPtr Data {
            get {
                CheckCreated();

                return Native.enet_packet_get_data(nativePacket);
            }
        }

        public int Length {
            get {
                CheckCreated();

                return Native.enet_packet_get_length(nativePacket);
            }
        }

        public bool HasReferences {
            get {
                CheckCreated();

                return Native.enet_packet_check_references(nativePacket) != 0;
            }
        }

        internal void CheckCreated()
        {
            if (nativePacket == IntPtr.Zero)
                throw new InvalidOperationException("Packet not created");
        }

        public void SetFreeCallback(IntPtr callback)
        {
            CheckCreated();

            Native.enet_packet_set_free_callback(nativePacket, callback);
        }

        public void SetFreeCallback(PacketFreeCallback callback)
        {
            CheckCreated();

            Native.enet_packet_set_free_callback(nativePacket, Marshal.GetFunctionPointerForDelegate(callback));
        }

        public void Create(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            Create(data, data.Length);
        }

        public void Create(byte[] data, int length)
        {
            Create(data, length, PacketFlags.None);
        }

        public void Create(byte[] data, PacketFlags flags)
        {
            Create(data, data.Length, flags);
        }

        public void Create(byte[] data, int length, PacketFlags flags)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (length < 0 || length > data.Length)
                throw new ArgumentOutOfRangeException();

            nativePacket = Native.enet_packet_create(data, (IntPtr)length, flags);
        }

        public void Create(IntPtr data, int length, PacketFlags flags)
        {
            if (data == IntPtr.Zero)
                throw new ArgumentNullException("data");

            if (length < 0)
                throw new ArgumentOutOfRangeException();

            nativePacket = Native.enet_packet_create(data, (IntPtr)length, flags);
        }

        public void Create(byte[] data, int offset, int length, PacketFlags flags)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (offset < 0 || length < 0 || length > data.Length)
                throw new ArgumentOutOfRangeException();

            nativePacket = Native.enet_packet_create_offset(data, (IntPtr)length, (IntPtr)offset, flags);
        }

        public void Create(IntPtr data, int offset, int length, PacketFlags flags)
        {
            if (data == IntPtr.Zero)
                throw new ArgumentNullException("data");

            if (offset < 0 || length < 0)
                throw new ArgumentOutOfRangeException();

            nativePacket = Native.enet_packet_create_offset(data, (IntPtr)length, (IntPtr)offset, flags);
        }

        public void CopyTo(byte[] destination)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            Marshal.Copy(Data, destination, 0, Length);
        }
    }

    public class Host : IDisposable
    {
        private IntPtr nativeHost;

        internal IntPtr NativeData {
            get {
                return nativeHost;
            }

            set {
                nativeHost = value;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (nativeHost != IntPtr.Zero)
            {
                Native.enet_host_destroy(nativeHost);
                nativeHost = IntPtr.Zero;
            }
        }

        ~Host()
        {
            Dispose(false);
        }

        public bool IsSet {
            get {
                return nativeHost != IntPtr.Zero;
            }
        }

        public uint PeersCount {
            get {
                CheckCreated();

                return Native.enet_host_get_peers_count(nativeHost);
            }
        }

        public uint PacketsSent {
            get {
                CheckCreated();

                return Native.enet_host_get_packets_sent(nativeHost);
            }
        }

        public uint PacketsReceived {
            get {
                CheckCreated();

                return Native.enet_host_get_packets_received(nativeHost);
            }
        }

        public uint BytesSent {
            get {
                CheckCreated();

                return Native.enet_host_get_bytes_sent(nativeHost);
            }
        }

        public uint BytesReceived {
            get {
                CheckCreated();

                return Native.enet_host_get_bytes_received(nativeHost);
            }
        }

        internal void CheckCreated()
        {
            if (nativeHost == IntPtr.Zero)
                throw new InvalidOperationException("Host not created");
        }

        private void CheckChannelLimit(int channelLimit)
        {
            if (channelLimit < 0 || channelLimit > Library.maxChannelCount)
                throw new ArgumentOutOfRangeException("channelLimit");
        }

        public void Create()
        {
            Create(null, 1, 0);
        }

        public void Create(int bufferSize)
        {
            Create(null, 1, 0, 0, 0, bufferSize);
        }

        public void Create(Address? address, int peerLimit)
        {
            Create(address, peerLimit, 0);
        }

        public void Create(Address? address, int peerLimit, int channelLimit)
        {
            Create(address, peerLimit, channelLimit, 0, 0, 0);
        }

        public void Create(int peerLimit, int channelLimit)
        {
            Create(null, peerLimit, channelLimit, 0, 0, 0);
        }

        public void Create(int peerLimit, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth)
        {
            Create(null, peerLimit, channelLimit, incomingBandwidth, outgoingBandwidth, 0);
        }

        public void Create(Address? address, int peerLimit, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth)
        {
            Create(address, peerLimit, channelLimit, incomingBandwidth, outgoingBandwidth, 0);
        }

        public void Create(Address? address, int peerLimit, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth, int bufferSize)
        {
            if (nativeHost != IntPtr.Zero)
                throw new InvalidOperationException("Host already created");

            if (peerLimit < 0 || peerLimit > Library.maxPeers)
                throw new ArgumentOutOfRangeException("peerLimit");

            CheckChannelLimit(channelLimit);

            if (address != null)
            {
                ENetAddress nativeAddress = address.Value.NativeData;

                nativeHost = Native.enet_host_create(ref nativeAddress, (IntPtr)peerLimit, (IntPtr)channelLimit, incomingBandwidth, outgoingBandwidth, bufferSize);
            }
            else
            {
                nativeHost = Native.enet_host_create(IntPtr.Zero, (IntPtr)peerLimit, (IntPtr)channelLimit, incomingBandwidth, outgoingBandwidth, bufferSize);
            }

            if (nativeHost == IntPtr.Zero)
                throw new InvalidOperationException("Host creation call failed");
        }

        public void EnableCompression()
        {
            CheckCreated();

            Native.enet_host_enable_compression(nativeHost);
        }

        public void PreventConnections(bool state)
        {
            CheckCreated();

            Native.enet_host_prevent_connections(nativeHost, (byte)(state ? 1 : 0));
        }

        public void Broadcast(byte channelID, ref Packet packet)
        {
            CheckCreated();

            packet.CheckCreated();
            Native.enet_host_broadcast(nativeHost, channelID, packet.NativeData);
            packet.NativeData = IntPtr.Zero;
        }

        public void Broadcast(byte channelID, ref Packet packet, Peer excludedPeer)
        {
            CheckCreated();

            packet.CheckCreated();
            Native.enet_host_broadcast_exclude(nativeHost, channelID, packet.NativeData, excludedPeer.NativeData);
            packet.NativeData = IntPtr.Zero;
        }

        public void Broadcast(byte channelID, ref Packet packet, Peer[] peers)
        {
            CheckCreated();

            packet.CheckCreated();

            if (peers.Length > 0)
            {
                IntPtr[] nativePeers = ArrayPool.GetPointerBuffer();
                int nativeCount = 0;

                for (int i = 0; i < peers.Length; i++)
                {
                    if (peers[i].NativeData != IntPtr.Zero)
                    {
                        nativePeers[nativeCount] = peers[i].NativeData;
                        nativeCount++;
                    }
                }

                Native.enet_host_broadcast_selective(nativeHost, channelID, packet.NativeData, nativePeers, (IntPtr)nativeCount);
            }

            packet.NativeData = IntPtr.Zero;
        }

        public int CheckEvents(out Event @event)
        {
            CheckCreated();


            int result = Native.enet_host_check_events(nativeHost, out ENetEvent nativeEvent);

            if (result <= 0)
            {
                @event = default;

                return result;
            }

            @event = new Event(nativeEvent);

            return result;
        }

        public Peer Connect(Address address)
        {
            return Connect(address, 0, 0);
        }

        public Peer Connect(Address address, int channelLimit)
        {
            return Connect(address, channelLimit, 0);
        }

        public Peer Connect(Address address, int channelLimit, uint data)
        {
            CheckCreated();
            CheckChannelLimit(channelLimit);

            ENetAddress nativeAddress = address.NativeData;
            Peer peer = new Peer(Native.enet_host_connect(nativeHost, ref nativeAddress, (IntPtr)channelLimit, data));

            if (peer.NativeData == IntPtr.Zero)
                throw new InvalidOperationException("Host connect call failed");

            return peer;
        }

        public int Service(int timeout, out Event @event)
        {
            if (timeout < 0)
                throw new ArgumentOutOfRangeException("timeout");

            CheckCreated();


            int result = Native.enet_host_service(nativeHost, out ENetEvent nativeEvent, (uint)timeout);

            if (result <= 0)
            {
                @event = default;

                return result;
            }

            @event = new Event(nativeEvent);

            return result;
        }

        public void SetBandwidthLimit(uint incomingBandwidth, uint outgoingBandwidth)
        {
            CheckCreated();

            Native.enet_host_bandwidth_limit(nativeHost, incomingBandwidth, outgoingBandwidth);
        }

        public void SetChannelLimit(int channelLimit)
        {
            CheckCreated();
            CheckChannelLimit(channelLimit);

            Native.enet_host_channel_limit(nativeHost, (IntPtr)channelLimit);
        }

        public void Flush()
        {
            CheckCreated();

            Native.enet_host_flush(nativeHost);
        }
    }

    public struct Peer
    {
        private IntPtr nativePeer;
        private readonly uint nativeID;

        internal IntPtr NativeData {
            get {
                return nativePeer;
            }

            set {
                nativePeer = value;
            }
        }

        internal Peer(IntPtr peer)
        {
            nativePeer = peer;
            nativeID = nativePeer != IntPtr.Zero ? Native.enet_peer_get_id(nativePeer) : 0;
        }

        public bool IsSet {
            get {
                return nativePeer != IntPtr.Zero;
            }
        }

        public uint ID {
            get {
                return nativeID;
            }
        }

        public string IP {
            get {
                CheckCreated();

                byte[] ip = ArrayPool.GetByteBuffer();

                if (Native.enet_peer_get_ip(nativePeer, ip, (IntPtr)ip.Length) == 0)
                    return Encoding.ASCII.GetString(ip, 0, ip.StringLength());
                else
                    return String.Empty;
            }
        }

        public ushort Port {
            get {
                CheckCreated();

                return Native.enet_peer_get_port(nativePeer);
            }
        }

        public uint MTU {
            get {
                CheckCreated();

                return Native.enet_peer_get_mtu(nativePeer);
            }
        }

        public PeerState State {
            get {
                return nativePeer == IntPtr.Zero ? PeerState.Uninitialized : Native.enet_peer_get_state(nativePeer);
            }
        }

        public uint RoundTripTime {
            get {
                CheckCreated();

                return Native.enet_peer_get_rtt(nativePeer);
            }
        }

        public uint LastSendTime {
            get {
                CheckCreated();

                return Native.enet_peer_get_lastsendtime(nativePeer);
            }
        }

        public uint LastReceiveTime {
            get {
                CheckCreated();

                return Native.enet_peer_get_lastreceivetime(nativePeer);
            }
        }

        public ulong PacketsSent {
            get {
                CheckCreated();

                return Native.enet_peer_get_packets_sent(nativePeer);
            }
        }

        public ulong PacketsLost {
            get {
                CheckCreated();

                return Native.enet_peer_get_packets_lost(nativePeer);
            }
        }

        public ulong BytesSent {
            get {
                CheckCreated();

                return Native.enet_peer_get_bytes_sent(nativePeer);
            }
        }

        public ulong BytesReceived {
            get {
                CheckCreated();

                return Native.enet_peer_get_bytes_received(nativePeer);
            }
        }

        public IntPtr Data {
            get {
                CheckCreated();

                return Native.enet_peer_get_data(nativePeer);
            }

            set {
                CheckCreated();

                Native.enet_peer_set_data(nativePeer, value);
            }
        }

        internal void CheckCreated()
        {
            if (nativePeer == IntPtr.Zero)
                throw new InvalidOperationException("Peer not created");
        }

        public void ConfigureThrottle(uint interval, uint acceleration, uint deceleration, uint threshold)
        {
            CheckCreated();

            Native.enet_peer_throttle_configure(nativePeer, interval, acceleration, deceleration, threshold);
        }

        public bool Send(byte channelID, ref Packet packet)
        {
            CheckCreated();

            packet.CheckCreated();

            return Native.enet_peer_send(nativePeer, channelID, packet.NativeData) == 0;
        }

        // Added by Coburn. This version returns either 0 if the send was successful,
        // or the ENET return code if not. Sometimes a bool is not enough to determine
        // the root cause of the issue.
        public int SendAndReturnStatusCode(byte channelID, ref Packet packet)
        {
            CheckCreated();

            packet.CheckCreated();

            return Native.enet_peer_send(nativePeer, channelID, packet.NativeData);
        }


        public bool Receive(out byte channelID, out Packet packet)
        {
            CheckCreated();

            IntPtr nativePacket = Native.enet_peer_receive(nativePeer, out channelID);

            if (nativePacket != IntPtr.Zero)
            {
                packet = new Packet(nativePacket);

                return true;
            }

            packet = default;

            return false;
        }

        public void Ping()
        {
            CheckCreated();

            Native.enet_peer_ping(nativePeer);
        }

        public void PingInterval(uint interval)
        {
            CheckCreated();

            Native.enet_peer_ping_interval(nativePeer, interval);
        }

        public void Timeout(uint timeoutLimit, uint timeoutMinimum, uint timeoutMaximum)
        {
            CheckCreated();

            Native.enet_peer_timeout(nativePeer, timeoutLimit, timeoutMinimum, timeoutMaximum);
        }

        public void Disconnect(uint data)
        {
            CheckCreated();

            Native.enet_peer_disconnect(nativePeer, data);
        }

        public void DisconnectNow(uint data)
        {
            CheckCreated();

            Native.enet_peer_disconnect_now(nativePeer, data);
        }

        public void DisconnectLater(uint data)
        {
            CheckCreated();

            Native.enet_peer_disconnect_later(nativePeer, data);
        }

        public void Reset()
        {
            CheckCreated();

            Native.enet_peer_reset(nativePeer);
        }
    }

    public static class Extensions
    {
        public static int StringLength(this byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            int i;

            for (i = 0; i < data.Length && data[i] != 0; i++) ;

            return i;
        }
    }

    public static class Library
    {
        public const uint maxChannelCount = 0xFF;
        public const uint maxPeers = 0xFFF;
        public const uint maxPacketSize = 32 * 1024 * 1024;
        public const uint throttleScale = 32;
        public const uint throttleAcceleration = 2;
        public const uint throttleDeceleration = 2;
        public const uint throttleInterval = 5000;
        public const uint timeoutLimit = 32;
        public const uint timeoutMinimum = 5000;
        public const uint timeoutMaximum = 30000;
        public const uint version = (2 << 16) | (3 << 8) | (0);

        public static bool Initialize()
        {
            return Native.enet_initialize() == 0;
        }

        public static bool Initialize(Callbacks inits)
        {
            return Native.enet_initialize_with_callbacks(version, inits.NativeData) == 0;
        }

        public static void Deinitialize()
        {
            Native.enet_deinitialize();
        }

        public static uint Time {
            get {
                return Native.enet_time_get();
            }
        }
    }

    [SuppressUnmanagedCodeSecurity]
    internal static class Native
    {
#if __IOS__ || UNITY_IOS && !UNITY_EDITOR
        // iOS
		private const string nativeLibrary = "__Internal";
#elif __APPLE__ || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        // MacOS
        // Custom ENet Repo builds as libenet.bundle; make sure it's the same.
        private const string nativeLibrary = "libenet";
#else
        // Assume everything else, Windows et al...
        // This might be interesting if someone's building for Nintendo Switch or whatnot...
        private const string nativeLibrary = "enet";
#endif

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_initialize();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_initialize_with_callbacks(uint version, ENetCallbacks inits);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_deinitialize();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_time_get();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_set_host_ip(ref ENetAddress address, string ip);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_set_host(ref ENetAddress address, string hostName);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_get_host_ip(ENetAddress address, StringBuilder ip, IntPtr ipLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_get_host(ENetAddress address, StringBuilder hostName, IntPtr nameLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_create(byte[] data, IntPtr dataLength, PacketFlags flags);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_create(IntPtr data, IntPtr dataLength, PacketFlags flags);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_create_offset(byte[] data, IntPtr dataLength, IntPtr dataOffset, PacketFlags flags);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_create_offset(IntPtr data, IntPtr dataLength, IntPtr dataOffset, PacketFlags flags);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_packet_check_references(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_get_data(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_packet_get_length(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_packet_set_free_callback(IntPtr packet, IntPtr callback);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_packet_dispose(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_host_create(ref ENetAddress address, IntPtr peerLimit, IntPtr channelLimit, uint incomingBandwidth, uint outgoingBandwidth, int bufferSize);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_host_create(IntPtr address, IntPtr peerLimit, IntPtr channelLimit, uint incomingBandwidth, uint outgoingBandwidth, int bufferSize);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_host_connect(IntPtr host, ref ENetAddress address, IntPtr channelCount, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_broadcast(IntPtr host, byte channelID, IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_broadcast_exclude(IntPtr host, byte channelID, IntPtr packet, IntPtr excludedPeer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_broadcast_selective(IntPtr host, byte channelID, IntPtr packet, IntPtr[] peers, IntPtr peersLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_service(IntPtr host, out ENetEvent @event, uint timeout);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_check_events(IntPtr host, out ENetEvent @event);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_channel_limit(IntPtr host, IntPtr channelLimit);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_bandwidth_limit(IntPtr host, uint incomingBandwidth, uint outgoingBandwidth);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_peers_count(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_packets_sent(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_packets_received(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_bytes_sent(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_bytes_received(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_flush(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_destroy(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_enable_compression(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_prevent_connections(IntPtr host, byte state);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_throttle_configure(IntPtr peer, uint interval, uint acceleration, uint deceleration, uint threshold);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_id(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_peer_get_ip(IntPtr peer, byte[] ip, IntPtr ipLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort enet_peer_get_port(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_mtu(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern PeerState enet_peer_get_state(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_rtt(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_lastsendtime(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_lastreceivetime(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong enet_peer_get_packets_sent(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong enet_peer_get_packets_lost(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong enet_peer_get_bytes_sent(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong enet_peer_get_bytes_received(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_peer_get_data(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_set_data(IntPtr peer, IntPtr data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_peer_send(IntPtr peer, byte channelID, IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_peer_receive(IntPtr peer, out byte channelID);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_ping(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_ping_interval(IntPtr peer, uint pingInterval);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_timeout(IntPtr peer, uint timeoutLimit, uint timeoutMinimum, uint timeoutMaximum);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_disconnect(IntPtr peer, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_disconnect_now(IntPtr peer, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_disconnect_later(IntPtr peer, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_reset(IntPtr peer);
    }
}
