// Ignorance 1.4.x LTS (Long Term Support)
// https://github.com/SoftwareGuy/Ignorance
// -----------------
// Copyright (c) 2019 - 2021 Matt Coburn (SoftwareGuy/Coburn64)
// Ignorance is licensed under the MIT license. Refer
// to the LICENSE file for more information.
using ENet;
using IgnoranceCore;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IgnoranceTransport
{
    [DisallowMultipleComponent]
    public class Ignorance : Transport
    {
        #region Inspector options
        [Header("Essentials")]
        public int port = 7777;

        [Header("Debug & Logging")]
        [Tooltip("Uses OnGUI to present you with statistics for Server and Client backend instances.")]
        public bool DebugDisplay = false;
        [Tooltip("How verbose do you want Ignorance to be?")]
        public IgnoranceLogType LogType = IgnoranceLogType.Standard;

        [Header("Server Configuration")]
        [Tooltip("Should the server bind to all interfaces?")]
        public bool serverBindsAll = true;
        [Tooltip("This is only used if Server Binds All is unticked.")]
        public string serverBindAddress = string.Empty;
        [Tooltip("How many peers native ENet will support. Low sane numbers help performance, avoids looping over huge native arrays. Recommended: maximum Mirror players, rounded to nearest 10. (Example: 16 -> 20).")]
        public int serverMaxPeerCapacity = 64;
        [Tooltip("Server network performance/CPU utilization trade off. Lower numbers = Better performance, more CPU. Higher numbers = Potentially lower performance, less CPU. (Value is in milliseconds)")]
        public int serverMaxNativeWaitTime = 1;
        [Tooltip("Interval between asking ENet for server status updates. Set to <= 0 to disable.")]
        public int serverStatusUpdateInterval = 0;

        [Header("Client Configuration")]
        [Tooltip("Client network performance/CPU utilization trade off. Lower numbers = Better performance, more CPU. Higher numbers = Potentially lower performance, less CPU. (Value is in milliseconds)")]
        public int clientMaxNativeWaitTime = 3;
        [Tooltip("Interval between asking ENet for client status updates. Set to <= 0 to disable.")]
        public int clientStatusUpdateInterval = 0;

        [Header("Channel Configuration")]
        [Tooltip("You must define your channels in the array shown here, otherwise ENet will not know what channel delivery type to use.")]
        public IgnoranceChannelTypes[] Channels;

        [Header("Ring Buffer Tweaking")]
        [Tooltip("[Client Only] Capacity of the incoming and outgoing ring buffers. If the ring buffer is full, it will spin waiting for a free slot in the buffer. Test and increase as required. This value translates to packets per second under a worse-case scenario.")]
        public int ClientDataBufferSize = 1000;
        [Tooltip("[Client Only] Capacity of the connection event buffer. This is probably best to keep small as connection events are literally minimal in Mirror.")]
        public int ClientConnEventBufferSize = 10;

        [Tooltip("[Server Only] Capacity of Server Incoming/Outgoing ring buffers. If the ring buffer is full, it will spin waiting for a free slot in the RingBuffer. Test and increase as required. This value translates to packets per second under a worse-case scenario.\n\n" +
            "Unlike the client value, it is recommended that you keep this resonably high as servers process more network IO than clients.")]
        public int ServerDataBufferSize = 5000;
        [Tooltip("[Server Only] Defines the capacity of server connection event buffer. This is probably best to keep moderately small unless you expect to have a large influx of users connecting/disconnecting at once.")]
        public int ServerConnEventBufferSize = 100;

        [Header("Danger: I hope you know what you're doing!")]
        [Tooltip("Used internally to keep allocations to a minimum. This is how much memory will be consumed by the packet buffer on startup, and then reused. If you have large packets, change this to something larger. Default is 4096 bytes (4KB).")]
        public int PacketBufferCapacity = 4096;
        [Tooltip("For UDP based protocols, it's best to keep your data under the safe MTU of 1200 bytes. This is the maximum allowed packet size, however note that internally ENet can only go to 32MB.")]
        public int MaxAllowedPacketSize = 33554432;
        #endregion

        #region Public Statistics
        public IgnoranceClientStats ClientStatistics;
        public IgnoranceServerStats ServerStatistics;
        #endregion

        public override bool Available()
        {
            // Ignorance is not available for Unity WebGL, the PS4 (no dev kit to confirm) or Switch (port exists but I have no access to said code).
            // Ignorance is available for most other operating systems.
#if UNITY_WEBGL || UNITY_PS4 || UNITY_PS5 || UNITY_SWITCH
            return false;
#else
            return true;
#endif
        }

        public void Awake()
        {
            if (LogType != IgnoranceLogType.Quiet)
                print($"Ignorance {IgnoranceInternals.Version} has arrived. Keep up to date, report bugs and support the developer at https://github.com/SoftwareGuy/Ignorance!");
        }

        public override string ToString()
        {
            return $"Ignorance v{IgnoranceInternals.Version}";
        }

        public override void ClientConnect(string address)
        {
            if (LogType != IgnoranceLogType.Quiet)
                print($"Ignorance: Requesting client thread dispatch for a connection to '{address}'");

            ClientState = ConnectionState.Connecting;
            cachedConnectionAddress = address;

            // Initialize.
            InitializeClientBackend();

            // Get going.            
            ignoreDataPackets = false;

            // Start!
            Client.Start();
        }

        public override void ClientConnect(Uri uri)
        {
            if (uri.Scheme != IgnoranceInternals.Scheme)
                throw new ArgumentException($"You used an invalid URI: {uri}. Please use {IgnoranceInternals.Scheme}://host:port instead", nameof(uri));

            if (!uri.IsDefaultPort)
                // Set the communication port to the one specified.
                port = uri.Port;

            // Pass onwards to the proper handler.
            ClientConnect(uri.Host);
        }

        public override bool ClientConnected() => ClientState == ConnectionState.Connected;

        public override void ClientDisconnect()
        {
            // 2022-01-17 Fix issue ticket #83
            if (Client != null)
            {
                // Ugh this feels like a ugly hack
                // If we're host client for example, we don't need to run these routines.
                if (!Client.IsAlive) return;

                ClientState = ConnectionState.Disconnecting;

                // Fix for the Client commands RingBuffer not being initialized if we're in host mode.
                if (Client.Commands != null)
                    Client.Commands.Enqueue(new IgnoranceCommandPacket { Type = IgnoranceCommandType.ClientWantsToStop });

                Client.Stop();
            }
            else
            {
                throw new InvalidOperationException("Cannot disconnect the client instance. The Ignorance Client instance is null.");
            }

        }

        public override void ClientSend(ArraySegment<byte> segment, int channelId)
        {
            if (Client == null)
            {
                Debug.LogError("Client object is null, this shouldn't really happen but it did...");
                return;
            }

            if (channelId < 0 || channelId > Channels.Length)
            {
                Debug.LogError("Channel ID is out of bounds.");
                return;
            }

            // Create our struct...
            Packet clientOutgoingPacket = default;
            int byteCount = segment.Count;
            int byteOffset = segment.Offset;

            // Set our desired flags...
            PacketFlags desiredFlags = (PacketFlags)Channels[channelId];

            // Warn if over recommended MTU...
            bool flagsSet = (desiredFlags & ReliableOrUnreliableFragmented) > 0;

            if (LogType != IgnoranceLogType.Quiet && byteCount > 1200 && !flagsSet)
                Debug.LogWarning($"Ignorance: Client tried to send a Unreliable packet bigger than the recommended ENet 1200 byte MTU ({byteCount} > 1200). ENet will force Reliable Fragmented delivery.");

            // Create the packet.
            clientOutgoingPacket.Create(segment.Array, byteOffset, byteCount + byteOffset, desiredFlags);

            // Enqueue the packet.
            IgnoranceOutgoingPacket dispatchPacket = new IgnoranceOutgoingPacket
            {
                Channel = (byte)channelId,
                Payload = clientOutgoingPacket
            };

            // Pass the packet onto the thread for dispatch.
            Client.Outgoing.Enqueue(dispatchPacket);
        }

        public override bool ServerActive()
        {
            // Very simple check.
            return Server != null && Server.IsAlive;
        }

        // Current version Mirror server disconnection routine.
        public override void ServerDisconnect(int connectionId)
        {
            if (Server == null)
            {
                Debug.LogError("Cannot enqueue kick packet; our Server object is null. Something has gone wrong.");
                // Return here because otherwise we will get a NRE when trying to enqueue the kick packet.
                return;
            }

            // Enqueue the kick packet.
            IgnoranceCommandPacket kickPacket = new IgnoranceCommandPacket
            {
                Type = IgnoranceCommandType.ServerKickPeer,
                PeerId = (uint)connectionId - 1 // ENet's native peer ID will be ConnID - 1
            };

            // Pass the packet onto the thread for dispatch.
            Server.Commands.Enqueue(kickPacket);
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            if (peerConnectionData == null)
                return "(unavailable)";

            // Need to adjust the string...
            if (!string.IsNullOrEmpty(peerConnectionData[connectionId - 1].IP))
                return $"{peerConnectionData[connectionId - 1].IP}:{peerConnectionData[connectionId - 1].Port}";
            else
                return "(unavailable)";
        }

        // v1.4.0b6: Mirror rearranged the ServerSend params, so we need to apply a fix for that or
        // we end up using the obsoleted version. The obsolete version isn't a fatal error, but
        // it's best to stick with the new structures.
        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId)
        {
            if (Server == null)
            {
                Debug.LogError("Ignorance Server: Cannot enqueue data packet; our Server object is null. Something has gone wrong.");
                return;
            }

            if (channelId < 0 || channelId > Channels.Length)
            {
                Debug.LogError("Ignorance Server: Channel ID is out of bounds.");
                return;
            }

            // Packet Struct
            Packet serverOutgoingPacket = default;
            int byteCount = segment.Count;
            int byteOffset = segment.Offset;
            PacketFlags desiredFlags = (PacketFlags)Channels[channelId];

            // Warn if over recommended MTU
            bool flagsSet = (desiredFlags & ReliableOrUnreliableFragmented) > 0;

            if (LogType != IgnoranceLogType.Quiet && byteCount > 1200 && !flagsSet)
                Debug.LogWarning($"Ignorance Server: Trying to send a Unreliable packet bigger than the recommended ENet 1200 byte MTU ({byteCount} > 1200). ENet will force Reliable Fragmented delivery.");

            // Create the packet.
            serverOutgoingPacket.Create(segment.Array, byteOffset, byteCount + byteOffset, (PacketFlags)Channels[channelId]);

            // Enqueue the packet.
            IgnoranceOutgoingPacket dispatchPacket = new IgnoranceOutgoingPacket
            {
                Channel = (byte)channelId,
                NativePeerId = (uint)connectionId - 1, // ENet's native peer ID will be ConnID - 1
                Payload = serverOutgoingPacket
            };

            lock (Server) // UNITYSTATION CODE // Multithreading requires some locks sometime!!!, Please notice!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!, Before updating ignorance
            {
                Server.Outgoing.Enqueue(dispatchPacket);
            }
		}

        public override void ServerStart()
        {
            if (LogType != IgnoranceLogType.Quiet)
                Debug.Log("Ignorance Server: Instance starting up...");

            InitializeServerBackend();

            Server.Start();
        }

        public override void ServerStop()
        {
            if (Server != null)
            {
                if (LogType != IgnoranceLogType.Quiet)
                    Debug.Log("Ignorance Server: Instance shutting down...");

                Server.Stop();
            }

            peerConnectionData = null;
        }

        public override Uri ServerUri()
        {
            UriBuilder builder = new UriBuilder
            {
                Scheme = IgnoranceInternals.Scheme,
                Host = serverBindAddress,
                Port = port
            };

            return builder.Uri;
        }

        public override void Shutdown()
        {
            // TODO: Nothing needed here?
        }

        // Check to ensure channels 0 and 1 mimic LLAPI. Override this at your own risk.
        private void OnValidate()
        {
            if (Channels != null && Channels.Length >= 2)
            {
                // Check to make sure that Channel 0 and 1 are correct.             
                if (Channels[0] != IgnoranceChannelTypes.Reliable)
                {
                    Debug.LogWarning("Please do not modify Ignorance Channel 0. The channel will be reset to Reliable delivery. If you need a channel with a different delivery, define and use it instead.");
                    Channels[0] = IgnoranceChannelTypes.Reliable;
                }

                if (Channels[1] != IgnoranceChannelTypes.Unreliable)
                {
                    Debug.LogWarning("Please do not modify Ignorance Channel 1. The channel will be reset to Unreliable delivery. If you need a channel with a different delivery, define and use it instead.");
                    Channels[1] = IgnoranceChannelTypes.Unreliable;
                }
            }
            else
            {
                Debug.LogWarning("Invalid Channels setting, fixing. If you've just added Ignorance to your NetworkManager GameObject, seeing this message is normal.");
                Channels = new IgnoranceChannelTypes[2]
                {
                    IgnoranceChannelTypes.Reliable,
                    IgnoranceChannelTypes.Unreliable
                };
            }

            // ENet only supports a maximum of 32MB packet size.
            if (MaxAllowedPacketSize > 33554432)
                MaxAllowedPacketSize = 33554432;
        }

        private void InitializeServerBackend()
        {
            if (Server == null)
            {
                Debug.LogWarning("Ignorance Server: Reference for Server mode was null. This shouldn't happen, but to be safe we'll attempt to reinitialize it.");
                Server = new IgnoranceServer();
            }

            // Set up the new IgnoranceServer reference.
            if (serverBindsAll)
            {
                // MacOS is special. It's also a massive thorn in my backside.
                Server.IsFruityDevice = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX;
                Server.BindAddress = IgnoranceInternals.BindAnyAddress;
                Server.BindAllInterfaces = true;
            }            
            else
                // Use the supplied bind address.
                Server.BindAddress = serverBindAddress;

            // Sets port, maximum peers, max channels, the server poll time, maximum packet size and verbosity.
            Server.BindPort = port;
            Server.MaximumPeers = serverMaxPeerCapacity;
            Server.MaximumChannels = Channels.Length;
            Server.PollTime = serverMaxNativeWaitTime;
            Server.MaximumPacketSize = MaxAllowedPacketSize;
            Server.Verbosity = (int)LogType;

            Server.IncomingOutgoingBufferSize = ServerDataBufferSize;
            Server.ConnectionEventBufferSize = ServerConnEventBufferSize;

            // Initializes the packet buffer.
            // Allocates once, that's it.
            if (InternalPacketBuffer == null)
                InternalPacketBuffer = new byte[PacketBufferCapacity];

            // This is required to ensure that ServerStatistics peer stats are initialised before first update
            if (ServerStatistics.PeerStats == null)
                ServerStatistics.PeerStats = new Dictionary<int, IgnoranceClientStats>(serverMaxPeerCapacity);

            // Setup the peer connection array.
            peerConnectionData = new PeerConnectionData[serverMaxPeerCapacity];
        }

        private void InitializeClientBackend()
        {
            if (Client == null)
            {
                Debug.LogWarning("Ignorance: Client backend instance reference was null. This shouldn't happen, but to be safe we'll reinitialize it.");
                Client = new IgnoranceClient();
            }

            // Sets address, port, channels to expect, verbosity, the server poll time and maximum packet size.
            Client.ConnectAddress = cachedConnectionAddress;
            Client.ConnectPort = port;
            Client.ExpectedChannels = Channels.Length;
            Client.PollTime = clientMaxNativeWaitTime;
            Client.MaximumPacketSize = MaxAllowedPacketSize;
            Client.Verbosity = (int)LogType;

            Client.IncomingOutgoingBufferSize = ClientDataBufferSize;
            Client.ConnectionEventBufferSize = ClientConnEventBufferSize;

            // Initializes the packet buffer. Allocates once, that's it.
            if (InternalPacketBuffer == null)
                InternalPacketBuffer = new byte[PacketBufferCapacity];
        }

        #region Main Thread Processing and Polling
        public void ServerPump()
        {
            // Process Server Events...
            if (Server.IsAlive)
            {
                ProcessServerPackets();

                if (serverStatusUpdateInterval > 0)
                {
                    serverStatusUpdateTimer += Time.deltaTime;

                    if (serverStatusUpdateTimer >= serverStatusUpdateInterval)
                    {
                        Server.Commands.Enqueue(new IgnoranceCommandPacket { Type = IgnoranceCommandType.ServerStatusRequest });
                        serverStatusUpdateTimer = 0f;
                    }
                }
            }
        }

        public void ClientPump()
        {
            // Only process client packets if we're not disconnected...
            if (ClientState != ConnectionState.Disconnected)
                ProcessClientPackets();

            // Used if we're connected and the client status update interval is above 0.
            if (ClientState == ConnectionState.Connected && clientStatusUpdateInterval > 0)
            {
                clientStatusUpdateTimer += Time.deltaTime;

                if (clientStatusUpdateTimer >= clientStatusUpdateInterval)
                {
                    Client.Commands.Enqueue(new IgnoranceCommandPacket { Type = IgnoranceCommandType.ClientStatusRequest });
                    clientStatusUpdateTimer = 0f;
                }
            }

        }

        private void ProcessServerPackets()
        {
            IgnoranceIncomingPacket incomingPacket;
            IgnoranceConnectionEvent connectionEvent;
            int adjustedConnectionId;
            Packet payload;
            IgnoranceServerStats serverStats;

            // Incoming connection events.
            while (Server.ConnectionEvents.TryDequeue(out connectionEvent))
            {
                adjustedConnectionId = (int)connectionEvent.NativePeerId + 1;

                if (LogType == IgnoranceLogType.Verbose)
                    Debug.Log($"Processing a server connection event from ENet native peer {connectionEvent.NativePeerId}. This peer would be Mirror ConnID {adjustedConnectionId}.");

                // Cache that peer.
                // NOTE: We cache the peers native id and do some magic later.
                peerConnectionData[(int)connectionEvent.NativePeerId] = new PeerConnectionData
                {
                    IP = connectionEvent.IP,
                    NativePeerId = connectionEvent.NativePeerId,
                    Port = connectionEvent.Port
                };

                OnServerConnected?.Invoke(adjustedConnectionId);
            }

            // Handle incoming data packets.
            // Console.WriteLine($"Server Incoming Queue is {Server.Incoming.Count}");
            while (Server.Incoming.TryDequeue(out incomingPacket))
            {
                adjustedConnectionId = (int)incomingPacket.NativePeerId + 1;
                payload = incomingPacket.Payload;

                int length = payload.Length;
                ArraySegment<byte> dataSegment;

                // Copy to working buffer and dispose of it.
                if (length > InternalPacketBuffer.Length)
                {
                    byte[] oneFreshNTastyGcAlloc = new byte[length];

                    payload.CopyTo(oneFreshNTastyGcAlloc);
                    dataSegment = new ArraySegment<byte>(oneFreshNTastyGcAlloc, 0, length);
                }
                else
                {
                    payload.CopyTo(InternalPacketBuffer);
                    dataSegment = new ArraySegment<byte>(InternalPacketBuffer, 0, length);
                }

                payload.Dispose();

                OnServerDataReceived?.Invoke(adjustedConnectionId, dataSegment, incomingPacket.Channel);
            }

            // Disconnection events.
            while (Server.DisconnectionEvents.TryDequeue(out IgnoranceConnectionEvent disconnectionEvent))
            {
                adjustedConnectionId = (int)disconnectionEvent.NativePeerId + 1;

                // The array is no longer occupied.
                peerConnectionData[(int)connectionEvent.NativePeerId] = default;

                if (LogType == IgnoranceLogType.Verbose)
                    Debug.Log($"Ignorance Server: Handling disconnection event from native peer {disconnectionEvent.NativePeerId}.");

                // Invoke Mirror handler.
                OnServerDisconnected?.Invoke(adjustedConnectionId);
            }

            // Handle status updates.
            if (Server.StatusUpdates.TryDequeue(out serverStats))
            {
                Server.RecycledServerStatBlocks.Enqueue(ServerStatistics);
                ServerStatistics = serverStats;
            }
        }


        private void ProcessClientPackets()
        {
            IgnoranceIncomingPacket incomingPacket;
            IgnoranceClientStats clientStats;
            Packet payload;

            // Handle connection events.
            while (Client.ConnectionEvents.TryDequeue(out IgnoranceConnectionEvent connectionEvent))
            {
                if (LogType == IgnoranceLogType.Verbose)
                    Debug.Log($"Ignorance Debug: Client processing a ConnectionEvents queue item. Type: {connectionEvent.EventType.ToString("{0:X2}")}");

                switch (connectionEvent.EventType)
                {
                    case 0x00:
                        // Connected to server.
                        ClientState = ConnectionState.Connected;

                        if (LogType != IgnoranceLogType.Quiet)
                            Debug.Log($"Ignorance: Client has successfully connected to server at {connectionEvent.IP}:{connectionEvent.Port}");

                        ignoreDataPackets = false;
                        OnClientConnected?.Invoke();
                        break;

                    case 0x01:
                        // Disconnected from server.
                        ClientState = ConnectionState.Disconnected;

                        if (LogType != IgnoranceLogType.Quiet)
                            Debug.Log($"Ignorance: Client has been disconnected from server.");

                        ignoreDataPackets = true;
                        OnClientDisconnected?.Invoke();
                        break;

                    default:
                        // Unknown type.
                        if (LogType != IgnoranceLogType.Quiet)
                            Debug.LogWarning($"Ignorance: Client has unknown connection event type {connectionEvent.EventType.ToString("{0:X2}")}.");
                        break;
                }
            }

            // Handle the incoming messages.
            while (Client.Incoming.TryDequeue(out incomingPacket))
            {
                // Temporary fix: if ENet thread is too fast for Mirror, then ignore the packet.
                // This is seen sometimes if you stop the client and there's still stuff in the queue.
                if (ignoreDataPackets)
                {
                    if (LogType == IgnoranceLogType.Verbose)
                        Debug.Log("Ignorance: Client ProcessClientPackets cycle skipped; ignoring data packet");
                    break;
                }


                // Otherwise client recieved data, advise Mirror.
                // print($"Byte array: {incomingPacket.RentedByteArray.Length}. Packet Length: {incomingPacket.Length}");
                payload = incomingPacket.Payload;
                int length = payload.Length;
                ArraySegment<byte> dataSegment;

                // Copy to working buffer and dispose of it.
                if (length > InternalPacketBuffer.Length)
                {
                    // Unity's favourite: A fresh 'n' tasty GC Allocation!
                    byte[] oneFreshNTastyGcAlloc = new byte[length];

                    payload.CopyTo(oneFreshNTastyGcAlloc);
                    dataSegment = new ArraySegment<byte>(oneFreshNTastyGcAlloc, 0, length);
                }
                else
                {
                    payload.CopyTo(InternalPacketBuffer);
                    dataSegment = new ArraySegment<byte>(InternalPacketBuffer, 0, length);
                }

                payload.Dispose();

                OnClientDataReceived?.Invoke(dataSegment, incomingPacket.Channel);
            }

            // Step 3: Handle status updates.
            if (Client.StatusUpdates.TryDequeue(out clientStats))
                ClientStatistics = clientStats;
        }
        #endregion


        #region Main Thread Processing and Polling - Ignorance Flavour
#if !IGNORANCE_MIRROR_POLLING
        // FixedUpdate can be called many times per frame.
        // Once we've handled stuff, we set a flag so that we don't poll again for this frame.
        private bool fixedUpdateCompletedWork;

        private void FixedUpdate()
        {
            if (fixedUpdateCompletedWork) return;

            ServerPump();
            ClientPump();

            // Flip the bool to signal we've done our work.
            fixedUpdateCompletedWork = true;
        }

        private new void Update()
        {
            // Process what FixedUpdate missed, only if the boolean is not set.
            if (!fixedUpdateCompletedWork)
            {
                ServerPump();
                ClientPump();
            }

            // Flip back the bool, so it can be reset.
            fixedUpdateCompletedWork = false;
        }
#endif
        #endregion

        #region Main Thread Processing and Polling - Mirror Flavour
#if IGNORANCE_MIRROR_POLLING
        public override void ClientEarlyUpdate()
        {
            ClientPump();
        }

        public override void ServerEarlyUpdate()
        {
            ServerPump();
        }
#endif
        #endregion

        #region Debug
        private void OnGUI()
        {
            if (DebugDisplay)
            {
                if (Client != null)
                    GUI.Box(new Rect(
                        new Vector2(32, Screen.height - 220), new Vector2(240, 100)),
                        "-- CLIENT --\n" +
                        $"State: {ClientState} ({(Client.IsAlive ? "Alive" : "Dead")}) \n" +
                        $"Round Trip Time: {ClientStatistics.RTT} \n" +
                        $"Bytes In/Out: {ClientStatistics.BytesReceived} / {ClientStatistics.BytesSent} \n" +
                        $"Queue In/Out: {(Client.Incoming != null ? $"{Client.Incoming.Count}" : "0")} / {(Client.Outgoing != null ? $"{Client.Outgoing.Count}" : "0")} \n" +
                        $"ConnEvents: {(Client.ConnectionEvents != null ? $"{Client.ConnectionEvents.Count}" : "0")}"
                    );

                if (Server != null)
                    GUI.Box(new Rect(
                        new Vector2(32, Screen.height - 110), new Vector2(240, 100)),

                        "-- SERVER --\n" +
                        $"State: {(Server.IsAlive ? "Alive" : "Dead")} \n" +
                        $"Bytes In/Out: {ServerStatistics.BytesReceived} / {ServerStatistics.BytesSent} \n" +
                        $"Queue In/Out: {(Server.Incoming != null ? $"{Server.Incoming.Count}" : "0")} / {(Server.Outgoing != null ? $"{Server.Outgoing.Count}" : "0")} \n" +
                        $"ConnEvents: {(Server.ConnectionEvents != null ? $"{Server.ConnectionEvents.Count}" : "0")}"
                    );

            }
        }
        #endregion

        // Mirror 46 (Mirror LTS) work arounds
        public override int GetMaxPacketSize(int channelId = 0)
        {
            bool isFragmentedAlready = ((PacketFlags)Channels[channelId] & ReliableOrUnreliableFragmented) > 0;
            return isFragmentedAlready ? MaxAllowedPacketSize : 1200;
        }

        #region Internals
        private bool ignoreDataPackets;
        private string cachedConnectionAddress = string.Empty;
        private IgnoranceServer Server = new IgnoranceServer();
        private IgnoranceClient Client = new IgnoranceClient();
        private PeerConnectionData[] peerConnectionData;

        private enum ConnectionState { Connecting, Connected, Disconnecting, Disconnected }
        private ConnectionState ClientState = ConnectionState.Disconnected;
        private byte[] InternalPacketBuffer;

        private const PacketFlags ReliableOrUnreliableFragmented = PacketFlags.Reliable | PacketFlags.UnreliableFragmented;
        private float clientStatusUpdateTimer = 0f;
        private float serverStatusUpdateTimer = 0f;
        #endregion
    }
}
