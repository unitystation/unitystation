// Ignorance 1.3.x
// A Unity LLAPI Replacement Transport for Mirror Networking
// https://github.com/SoftwareGuy/Ignorance
// -----------------
// Ignorance Transport is licensed under the MIT license, however
// it comes with no warranty what-so-ever. However, if you do
// encounter a problem with Ignorance you can get support by
// dropping past the Mirror discord's #ignorance channel. Otherwise,
// open a issue ticket on the GitHub issues page. Ensure you provide
// lots of detail of what you were doing and the error/stack trace.
// -----------------
// Server & Client Threaded Version
//
// -----------------
using UnityEngine;
using Debug = UnityEngine.Debug;

// Used for threading.
using System;
using System.Threading;

using System.Collections.Concurrent;
using System.Collections.Generic;

// Very important these ones.
using ENet;
using Event = ENet.Event;
using EventType = ENet.EventType;

namespace Mirror
{
    public class IgnoranceThreaded : Transport, ISegmentTransport
    {
        // DO NOT TOUCH THIS.
        public const string Scheme = "enet";

        // Client Queues
        static ConcurrentQueue<IncomingPacket> MirrorClientIncomingQueue = new ConcurrentQueue<IncomingPacket>();
        static ConcurrentQueue<OutgoingPacket> MirrorClientOutgoingQueue = new ConcurrentQueue<OutgoingPacket>();

        // Server Queues
        static ConcurrentQueue<IncomingPacket> MirrorServerIncomingQueue = new ConcurrentQueue<IncomingPacket>();    // queue going into mirror from clients.
        static ConcurrentQueue<OutgoingPacket> MirrorServerOutgoingQueue = new ConcurrentQueue<OutgoingPacket>();    // queue going to clients from Mirror.

        // lookup and reverse lookup dictionaries
        static ConcurrentDictionary<int, Peer> ConnectionIDToPeers = new ConcurrentDictionary<int, Peer>();
        static ConcurrentDictionary<Peer, int> PeersToConnectionIDs = new ConcurrentDictionary<Peer, int>();

        // Threads
        static Thread serverWorker;
        static Thread clientWorker;

        static volatile bool serverShouldCeaseOperation, clientShouldCeaseOperation;
        static volatile bool ServerStarted, ClientStarted;

        // Client stuffs.
        static volatile bool isClientConnected = false;
        static volatile string clientConnectionAddress = string.Empty;

        // Standard stuffs
        private bool ENETInitialized = false;
        // Properties
        public bool DebugEnabled;

        [Header("UDP Server and Client Settings")]
        public bool ServerBindAll = true;
        public string ServerBindAddress = "127.0.0.1";
        public int CommunicationPort = 7777;
        public int MaximumPeerCCU = 4095;

        [Header("Thread Settings")]
        public int EnetPollTimeout = 1;

        [Header("Security")]
        [UnityEngine.Serialization.FormerlySerializedAs("MaxPacketSize")]
        public int MaxPacketSizeInKb = 64;

        [Header("Channel Definitions")]
        public IgnoranceChannelTypes[] Channels;

        // Standard things
        public void Awake()
        {
            print("Thanks for using Ignorance Threaded Edition! If you experience bugs with this version, please file a GitHub support ticket. https://github.com/SoftwareGuy/Ignorance");

            if (MaximumPeerCCU > 4095)
            {
                Debug.LogWarning("WARNING: You cannot have more than 4096 peers with this transport. While this is an artificial limitation and more peers are technically supported, it is a limitation of the underlying C library.");
                Debug.LogWarning("Do not file a bug report regarding this. There's a valid reason why 4096 is the maximum limit.");
                MaximumPeerCCU = 4095;
            }
        }

        public override bool Available()
        {
#if UNITY_WEBGL
            // Ignorance is not available on these platforms.
            return false;
#else
            return true;
#endif
        }

        public override string ToString()
        {
            return "Ignorance Threaded";
        }

        public void LateUpdate()
        {
            if (enabled)
            {
                // Server will pump itself...
                if (ServerStarted) ProcessServerMessages();
                if (ClientStarted) ProcessClientMessages();
            }
        }

        private bool InitializeENET()
        {
            return Library.Initialize();
        }

        // Server processing loop.
        private bool ProcessServerMessages()
        {
            // Get to the queue! Check those corners!
            while (MirrorServerIncomingQueue.TryDequeue(out IncomingPacket pkt))
            {
                switch (pkt.type)
                {
                    case MirrorPacketType.ServerClientConnected:
                        OnServerConnected?.Invoke(pkt.connectionId);
                        break;
                    case MirrorPacketType.ServerClientDisconnected:
                        OnServerDisconnected?.Invoke(pkt.connectionId);
                        break;
                    case MirrorPacketType.ServerClientSentData:
                        OnServerDataReceived?.Invoke(pkt.connectionId, new ArraySegment<byte>(pkt.data), pkt.channelId);
                        break;
                    default:
                        // Nothing to see here.
                        break;
                }
            }

            // Flashbang though the window and race to the finish.
            return true;
        }

        #region Client Portion
        private bool ProcessClientMessages()
        {
            while (MirrorClientIncomingQueue.TryDequeue(out IncomingPacket pkt))
            {
                switch (pkt.type)
                {
                    case MirrorPacketType.ClientConnected:
                        if (DebugEnabled) print($"Ignorance: We have connected!");
                        isClientConnected = true;
                        OnClientConnected?.Invoke();
                        break;
                    case MirrorPacketType.ClientDisconnected:
                        if (DebugEnabled) print($"Ignorance: We have been disconnected.");
                        isClientConnected = false;
                        OnClientDisconnected?.Invoke();
                        break;
                    case MirrorPacketType.ClientGotData:
                        OnClientDataReceived?.Invoke(new ArraySegment<byte>(pkt.data), pkt.channelId);
                        break;
                }
            }
            return true;
        }

        // Is the client connected?
        public override bool ClientConnected()
        {
            return isClientConnected;
        }

        public override void ClientConnect(string address)
        {
            // initialize
            if (!ENETInitialized)
            {
                if (InitializeENET())
                {
                    Debug.Log($"Ignorance successfully initialized ENET.");
                    ENETInitialized = true;
                }
                else
                {
                    Debug.LogError($"Ignorance failed to initialize ENET! Cannot continue.");
                    return;
                }
            }

            if (Channels.Length > 255)
            {
                Debug.LogError($"Ignorance: Too many channels. Channel limit is 255, you have {Channels.Length}. Aborting connection.");
                return;
            }

            if (CommunicationPort < ushort.MinValue || CommunicationPort > ushort.MaxValue)
            {
                Debug.LogError($"Ignorance: Bad communication port number. You need to set it between port 0 and 65535. Aborting connection.");
                return;
            }

            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"Ignorance: Null or empty address to connect to. Aborting connection.");
                return;
            }

            clientConnectionAddress = address;
            clientShouldCeaseOperation = false;

            // Important: clean the concurrentqueues
            MirrorClientIncomingQueue = new ConcurrentQueue<IncomingPacket>();
            MirrorClientOutgoingQueue = new ConcurrentQueue<OutgoingPacket>();

            print($"Ignorance: Starting connection to {clientConnectionAddress}...");
            clientWorker = IgnoranceClientThread();
            clientWorker.Start();
        }

        // Client Sending: ArraySegment and classic byte array versions
        public override bool ClientSend(int channelId, ArraySegment<byte> data)
        {
            return ENETClientQueueInternal(channelId, data);
        }

        public override void ClientDisconnect()
        {
            if (DebugEnabled) Debug.Log($"Ignorance: Client disconnection acknowledged");

            if (ServerStarted)
            {
                Debug.LogWarning("MIRROR BUG: ClientDisconnect called even when we're in HostClient/Dedicated Server mode");
                return;
            }

            OutgoingPacket opkt = default;
            opkt.commandType = CommandPacketType.ClientDisconnectRequest;
            MirrorClientOutgoingQueue.Enqueue(opkt);

            // ...
        }

        public override Uri ServerUri()
        {
	        return new Uri(ServerBindAddress);
        }

        #endregion

        #region Server Portion
        public override bool ServerActive()
        {
            return ServerStarted;
        }

        public override void ServerStart()
        {
            print($"Ignorance Threaded: Starting server worker.");

            serverShouldCeaseOperation = false;
            serverWorker = IgnoranceServerThread();
            serverWorker.Start();
        }

        // Can't deprecate this due to Dissonance...
        public bool ServerSend(int connectionId, int channelId, ArraySegment<byte> data)
        {
            return ENETServerQueueInternal(connectionId, channelId, data);
        }

        public override bool ServerDisconnect(int connectionId)
        {
            OutgoingPacket op = default;
            op.connectionId = connectionId;
            op.commandType = CommandPacketType.BootToTheFace;
            return true;
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            return "UNKNOWN";
        }

        public override void ServerStop()
        {
            serverShouldCeaseOperation = true;
            Thread.Sleep(5);    // Allow it to have a micro-sleep
            if (serverWorker != null && serverWorker.IsAlive) serverWorker.Join();

            // IMPORTANT: Flush the queues. Get rid of the dead bodies.
            // c6: Do not use new, instead just while dequeue anything else in the queue
            // c6: helps avoid GC
            while (MirrorServerIncomingQueue.TryDequeue(out _))
            {
                ;
            }

            while (MirrorServerOutgoingQueue.TryDequeue(out _))
            {
                ;
            }

            print($"Ignorance Threaded: Server stopped.");
        }

        public override void Shutdown()
        {
            serverShouldCeaseOperation = true;
            clientShouldCeaseOperation = true;

            if (serverWorker != null && serverWorker.IsAlive) serverWorker.Join();
            if (clientWorker != null && clientWorker.IsAlive) clientWorker.Join();

            if (ENETInitialized) Library.Deinitialize();
            ENETInitialized = false;
        }
        #endregion

        #region General Purpose
        public override int GetMaxPacketSize(int channelId = 0)
        {
            return MaxPacketSizeInKb * 1024;
        }
        #endregion

        #region Client Threading
        private Thread IgnoranceClientThread()
        {
            ThreadBootstrapStruct threadBootstrap = new ThreadBootstrapStruct
            {
                hostAddress = clientConnectionAddress,
                port = (ushort)CommunicationPort,
                maxChannels = Channels.Length,
                maxPacketSize = MaxPacketSizeInKb * 1024,
                threadPumpTimeout = EnetPollTimeout
            };

            Thread t = new Thread(() => ClientWorkerThread(threadBootstrap));
            return t;
        }

        private static void ClientWorkerThread(ThreadBootstrapStruct startupInfo)
        {
            // Setup...
            byte[] workerPacketBuffer = new byte[startupInfo.maxPacketSize];
            Address cAddress = new Address();

            // Drain anything in the queues...
            while (MirrorClientIncomingQueue.TryDequeue(out _))
            {
                ;
            }

            while (MirrorClientOutgoingQueue.TryDequeue(out _))
            {
                ;
            }

            // This comment was actually left blank, but now it's not. You're welcome.
            using (Host cHost = new Host())
            {
                try
                {
                    cHost.Create(null, 1, startupInfo.maxChannels, 0, 0, startupInfo.maxPacketSize);
                    ClientStarted = true;
                }
                catch (Exception e)
                {
                    Debug.LogError("Ignorance encountered a fatal exception. I'm sorry, but I gotta bail - if you believe you found a bug, please report it on the GitHub.\n" +
                        $"The exception returned was: {e.ToString()}");
                    return;
                }

                // Attempt to start connection...
                cAddress.SetHost(startupInfo.hostAddress);
                cAddress.Port = startupInfo.port;
                Peer cPeer = cHost.Connect(cAddress, startupInfo.maxChannels);

                while (!clientShouldCeaseOperation)
                {
                    bool clientWasPolled = false;

                    while (!clientWasPolled)
                    {
                        if (cHost.CheckEvents(out Event networkEvent) <= 0)
                        {
                            if (cHost.Service(startupInfo.threadPumpTimeout, out networkEvent) <= 0) break;
                            clientWasPolled = true;
                        }

                        switch (networkEvent.Type)
                        {
                            case EventType.Connect:
                                // Client connected.
                                IncomingPacket connPkt = default;
                                connPkt.type = MirrorPacketType.ClientConnected;
                                MirrorClientIncomingQueue.Enqueue(connPkt);
                                break;
                            case EventType.Timeout:
                            case EventType.Disconnect:
                                // Client disconnected.
                                IncomingPacket disconnPkt = default;
                                disconnPkt.type = MirrorPacketType.ClientDisconnected;
                                MirrorClientIncomingQueue.Enqueue(disconnPkt);
                                break;
                            case EventType.Receive:
                                // Client recieving some data.
                                if (!networkEvent.Packet.IsSet)
                                {
                                    print("Ignorance WARNING: A incoming packet is not set correctly.");
                                    break;
                                }

                                if (networkEvent.Packet.Length > workerPacketBuffer.Length)
                                {
                                    print($"Ignorance: Packet too big to fit in buffer. {networkEvent.Packet.Length} packet bytes vs {workerPacketBuffer.Length} cache bytes {networkEvent.Peer.ID}.");
                                    networkEvent.Packet.Dispose();
                                    break;
                                }
                                else
                                {
                                    // invoke on the client.
                                    try
                                    {
                                        networkEvent.Packet.CopyTo(workerPacketBuffer);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"Ignorance caught an exception while trying to copy data from the unmanaged (ENET) world to managed (Mono/IL2CPP) world. Please consider reporting this to the Ignorance developer on GitHub.\n" +
                                            $"Exception returned was: {e.Message}\n" +
                                            $"Debug details: {(workerPacketBuffer == null ? "packet buffer was NULL" : $"{workerPacketBuffer.Length} byte work buffer")}, {networkEvent.Packet.Length} byte(s) network packet length\n" +
                                            $"Stack Trace: {e.StackTrace}");
                                        networkEvent.Packet.Dispose();
                                        break;
                                    }

                                    int spLength = networkEvent.Packet.Length;

                                    IncomingPacket dataPkt = default;
                                    dataPkt.type = MirrorPacketType.ClientGotData;
                                    dataPkt.data = new byte[spLength];  // Grrr!!!
                                    networkEvent.Packet.CopyTo(dataPkt.data);

                                    MirrorClientIncomingQueue.Enqueue(dataPkt);
                                }
                                break;
                        }

                        networkEvent.Packet.Dispose();
                    }

                    // Outgoing stuff
                    while (MirrorClientOutgoingQueue.TryDequeue(out OutgoingPacket opkt))
                    {
                        if (opkt.commandType == CommandPacketType.ClientDisconnectRequest)
                        {
                            cPeer.DisconnectNow(0);
                            return;
                        }

                        int returnCode = cPeer.SendAndReturnStatusCode(opkt.channelId, ref opkt.payload);
                        if (returnCode != 0) print($"Ignorance: Could not send {opkt.payload.Length} bytes to server on channel {opkt.channelId}, error code {returnCode}");
                    }
                }

                cPeer.DisconnectNow(0);
                cHost.Flush();
                ClientStarted = false;
            }
        }
        #endregion

        #region Server Threading
        // Server thread.
        private Thread IgnoranceServerThread()
        {
            string bindAddress = string.Empty;

            ThreadBootstrapStruct startupInformation = new ThreadBootstrapStruct()
            {
                hostAddress = bindAddress,
                port = (ushort)CommunicationPort,
                maxPacketSize = MaxPacketSizeInKb * 1024,
                maxPeers = MaximumPeerCCU,
                maxChannels = Channels.Length,
                threadPumpTimeout = EnetPollTimeout
            };

            Thread t = new Thread(() => ServerWorkerThread(startupInformation));
            return t;
        }

        private static void ServerWorkerThread(ThreadBootstrapStruct startupInformation)
        {
            // Worker buffer.
            byte[] workerPacketBuffer = new byte[startupInformation.maxPacketSize];
            // Connection ID.
            int nextConnectionId = 1;
            // Server address properties
            Address eAddress = new Address()
            {
                Port = startupInformation.port,
            };

            // Bind on everything or not?
            if (!string.IsNullOrEmpty(startupInformation.hostAddress)) eAddress.SetHost(startupInformation.hostAddress);

            using (Host serverWorkerHost = new Host())
            {
                try
                {
                    serverWorkerHost.Create(eAddress, startupInformation.maxPeers, startupInformation.maxChannels, 0, 0, startupInformation.maxPacketSize);
                    ServerStarted = true;
                }
                catch (Exception e)
                {
                    Debug.LogError("Ignorance encountered a fatal exception. I'm sorry, but I gotta bail - if you believe you found a bug, please report it on the GitHub.\n" +
                        $"The exception returned was: {e.ToString()}");
                    return;
                }

                Debug.Log($"Ignorance Server worker thread is ready for connections! I'm listening on UDP port {startupInformation.port}.\n" +
                    $"Capacity: {startupInformation.maxPeers} peers with {startupInformation.maxChannels} channels. My buffer size is {startupInformation.maxPacketSize} bytes");

                // The meat and potatoes.
                while (!serverShouldCeaseOperation)
                {
                    // Outgoing stuff
                    while (MirrorServerOutgoingQueue.TryDequeue(out OutgoingPacket opkt))
                    {
                        switch (opkt.commandType)
                        {
                            case CommandPacketType.BootToTheFace:
                                if (ConnectionIDToPeers.TryGetValue(opkt.connectionId, out Peer bootedPeer))
                                {
                                    bootedPeer.DisconnectLater(0);
                                }
                                break;

                            case CommandPacketType.Nothing:
                            default:
                                if (ConnectionIDToPeers.TryGetValue(opkt.connectionId, out Peer target))
                                {
                                    int returnCode = target.SendAndReturnStatusCode(opkt.channelId, ref opkt.payload);
                                    if (returnCode != 0) print($"Could not send {opkt.payload.Length} bytes to target peer {target.ID} on channel {opkt.channelId}, error code {returnCode}");
                                }
                                break;
                        }
                    }

                    // Flush here?
                    serverWorkerHost.Flush();

                    // Incoming stuffs now.
                    bool hasBeenPolled = false;

                    while (!hasBeenPolled)
                    {
                        if (serverWorkerHost.CheckEvents(out Event netEvent) <= 0)
                        {
                            if (serverWorkerHost.Service(startupInformation.threadPumpTimeout, out netEvent) <= 0)
                                break;

                            hasBeenPolled = true;
                        }

                        switch (netEvent.Type)
                        {
                            case EventType.None:
                                break;

                            case EventType.Connect:
                                int connectionId = nextConnectionId;
                                nextConnectionId += 1;

                                // Add to dictionaries.
                                if (!PeersToConnectionIDs.TryAdd(netEvent.Peer, connectionId)) Debug.LogError($"ERROR: We already know this client in our Connection ID to Peer Mapping?!");
                                if (!ConnectionIDToPeers.TryAdd(connectionId, netEvent.Peer)) Debug.LogError($"ERROR: We already know this client in our Peer to ConnectionID Mapping?!");

                                // Send a message back to mirror.
                                IncomingPacket newConnectionPkt = default;
                                newConnectionPkt.connectionId = connectionId;
                                newConnectionPkt.type = MirrorPacketType.ServerClientConnected;
                                newConnectionPkt.ipAddress = netEvent.Peer.IP;

                                MirrorServerIncomingQueue.Enqueue(newConnectionPkt);
                                break;

                            case EventType.Disconnect:
                            case EventType.Timeout:
                                if (PeersToConnectionIDs.TryGetValue(netEvent.Peer, out int deadPeer))
                                {
                                    IncomingPacket disconnectionPkt = default;
                                    disconnectionPkt.connectionId = deadPeer;
                                    disconnectionPkt.type = MirrorPacketType.ServerClientDisconnected;
                                    disconnectionPkt.ipAddress = netEvent.Peer.IP;

                                    MirrorServerIncomingQueue.Enqueue(disconnectionPkt);
                                    ConnectionIDToPeers.TryRemove(deadPeer, out Peer _);
                                }

                                PeersToConnectionIDs.TryRemove(netEvent.Peer, out int _);
                                break;

                            case EventType.Receive:
                                int dataConnID = -1;

                                if (PeersToConnectionIDs.TryGetValue(netEvent.Peer, out dataConnID))
                                {
                                    if (!netEvent.Packet.IsSet)
                                    {
                                        print("Ignorance WARNING: A incoming packet is not set correctly - attempting to continue!");
                                        return;
                                    }

                                    if (netEvent.Packet.Length > startupInformation.maxPacketSize)
                                    {
                                        Debug.LogWarning($"Ignorance WARNING: Packet too large for buffer; dropping. Packet {netEvent.Packet.Length} bytes; limit is {startupInformation.maxPacketSize} bytes.");
                                        netEvent.Packet.Dispose();
                                        return;
                                    }

                                    // Copy to the packet cache.
                                    try
                                    {
                                        netEvent.Packet.CopyTo(workerPacketBuffer);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"Ignorance caught an exception while trying to copy data from the unmanaged (ENET) world to managed (Mono/IL2CPP) world. Please consider reporting this to the Ignorance developer on GitHub.\n" +
                                            $"Exception returned was: {e.Message}\n" +
                                            $"Debug details: {(workerPacketBuffer == null ? "packet buffer was NULL" : $"{workerPacketBuffer.Length} byte work buffer")}, {netEvent.Packet.Length} byte(s) network packet length\n" +
                                            $"Stack Trace: {e.StackTrace}");
                                        netEvent.Packet.Dispose();
                                        return;
                                    }

                                    int spLength = netEvent.Packet.Length;

                                    IncomingPacket dataPkt = default;
                                    dataPkt.connectionId = dataConnID;
                                    dataPkt.channelId = (int)netEvent.ChannelID;
                                    dataPkt.type = MirrorPacketType.ServerClientSentData;

                                    // TODO: Come up with a better method of doing this.
                                    dataPkt.data = new byte[spLength];
                                    try
                                    {
                                        Array.Copy(workerPacketBuffer, 0, dataPkt.data, 0, spLength);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"Ignorance caught an exception while copying data between buffers: {e.Message}\n" +
                                            $"Debug details: {(workerPacketBuffer == null ? " packet buffer was NULL" : $"{workerPacketBuffer.Length} byte work buffer")}, {(dataPkt.data == null ? "cache packet buffer was NULL" : $"{dataPkt.data.Length} byte(s) cache packet buffer length")}\n" +
                                            $"Stack Trace: {e.StackTrace}");

                                        netEvent.Packet.Dispose();
                                        return;
                                    }

                                    // Faulty .Array on the end seems to return the rest of the buffer as well instead of just 10 bytes or whatever
                                    // dataPkt.data = new ArraySegment<byte>(workerPacketBuffer, 0, spLength);
                                    dataPkt.ipAddress = netEvent.Peer.IP;

                                    MirrorServerIncomingQueue.Enqueue(dataPkt);
                                }
                                else
                                {
                                    // Kick the peer.
                                    netEvent.Peer.DisconnectNow(0);
                                }

                                netEvent.Packet.Dispose();
                                break;
                        }
                    }
                }

                // Disconnect everyone, we're done here.
                print($"Kicking all connected Peers...");
                foreach (KeyValuePair<int, Peer> kv in ConnectionIDToPeers) kv.Value.DisconnectNow(0);

                print("Flushing...");
                serverWorkerHost.Flush();
                ServerStarted = false;
            }
        }
        #endregion

        #region Mirror 6.2+ - URI Support
#if MIRROR_7_0_OR_NEWER
        public override Uri ServerUri()
        {
            UriBuilder builder = new UriBuilder();
            builder.Scheme = Scheme;
            builder.Host = ServerBindAddress;
            builder.Port = CommunicationPort;
            return builder.Uri;
        }
#endif
        public override void ClientConnect(Uri uri)
        {
            if (uri.Scheme != Scheme)
                throw new ArgumentException($"Invalid uri {uri}, use {Scheme}://host:port instead", nameof(uri));

            if (!uri.IsDefaultPort)
            {
                // Set the communication port to the one specified.
                CommunicationPort = uri.Port;
            }

            ClientConnect(uri.Host);
        }
        #endregion

        #region Unity Editor and Sanity Checks
        // Sanity checks.
        private void OnValidate()
        {
            if (Channels != null && Channels.Length >= 2)
            {
                // Check to make sure that Channel 0 and 1 are correct.
                if (Channels[0] != IgnoranceChannelTypes.Reliable) Channels[0] = IgnoranceChannelTypes.Reliable;
                if (Channels[1] != IgnoranceChannelTypes.Unreliable) Channels[1] = IgnoranceChannelTypes.Unreliable;
            }
            else
            {
                Channels = new IgnoranceChannelTypes[2]
                {
                    IgnoranceChannelTypes.Reliable,
                    IgnoranceChannelTypes.Unreliable
                };
            }
        }
        public override bool ServerSend(List<int> connectionIds, int channelId, ArraySegment<byte> segment)
        {
            if (!ServerStarted)
            {
                Debug.LogError("Attempted to send while the server was not active");
                return false;
            }

            if (channelId > Channels.Length)
            {
                Debug.LogWarning($"Ignorance: Attempted to send data on channel {channelId} when we only have {Channels.Length} channels defined");
                return false;
            }

            foreach (int conn in connectionIds)
            {
                // Another sneaky hack
                ENETServerQueueInternal(conn, channelId, segment);
            }

            return true;
        }

        /// <summary>
        /// Enqueues a packet for ENET worker to pick up and dispatch.
        /// Hopefully should make it easier to fix things.
        /// </summary>
        /// <param name="channelId">The channel id you wish to send the packet on. Must be within 0 and the count of the channels array.</param>
        /// <param name="dataPayload">The array segment containing the data to send to ENET.</param>
        /// <returns></returns>
        private bool ENETClientQueueInternal(int channelId, ArraySegment<byte> dataPayload)
        {
            if (channelId > Channels.Length)
            {
                Debug.LogWarning($"Ignorance: Attempted to send data on channel {channelId} when we only have {Channels.Length} channels defined");
                return false;
            }

            OutgoingPacket opkt = default;
            opkt.channelId = (byte)channelId;

            Packet payload = default;
            payload.Create(dataPayload.Array, dataPayload.Offset, dataPayload.Count + dataPayload.Offset, (PacketFlags)Channels[channelId]);

            opkt.payload = payload;

            // Enqueue it.
            MirrorClientOutgoingQueue.Enqueue(opkt);

            return true;
        }

        private bool ENETServerQueueInternal(int connectionId, int channelId, ArraySegment<byte> data)
        {
            if (!ServerStarted)
            {
                Debug.LogError("Attempted to send while the server was not active");
                return false;
            }

            if (channelId > Channels.Length)
            {
                Debug.LogWarning($"Ignorance: Attempted to send data on channel {channelId} when we only have {Channels.Length} channels defined");
                return false;
            }

            OutgoingPacket op = default;
            op.connectionId = connectionId;
            op.channelId = (byte)channelId;

            Packet dataPayload = default;
            dataPayload.Create(data.Array, data.Offset, data.Count + data.Offset, (PacketFlags)Channels[channelId]);

            op.payload = dataPayload;

            MirrorServerOutgoingQueue.Enqueue(op);
            return true;
        }
        #endregion

        #region Structs, classes, etc
        // Incoming packet struct.
        private struct IncomingPacket
        {
            public int connectionId;
            public int channelId;
            public MirrorPacketType type;
            public byte[] data;
            public string ipAddress;
        }
        // Outgoing packet struct
        private struct OutgoingPacket
        {
            public int connectionId;
            public byte channelId;
            public Packet payload;
            public CommandPacketType commandType;
        }

        // Packet Type Struct. Not to be confused with the ENET Packet Type.
        [Serializable]
        public enum MirrorPacketType
        {
            ServerClientConnected,
            ServerClientDisconnected,
            ServerClientSentData,
            ClientConnected,
            ClientDisconnected,
            ClientGotData
        }

        // Command Packet Type Struct.
        public enum CommandPacketType
        {
            Nothing,
            BootToTheFace,
            ClientDisconnectRequest
        }

        // -> Moved ChannelTypes enum to it's own file, so it's easier to maintain.

        public struct ThreadBootstrapStruct
        {
            public string hostAddress;
            public ushort port;

            public int threadPumpTimeout;

            public int maxPacketSize;
            public int maxChannels;
            public int maxPeers;
        }
        #endregion
    }


}
