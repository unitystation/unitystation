// Ignorance 1.4.x LTS (Long Term Support)
// https://github.com/SoftwareGuy/Ignorance
// -----------------
// Copyright (c) 2019 - 2021 Matt Coburn (SoftwareGuy/Coburn64)
// Ignorance is licensed under the MIT license. Refer
// to the LICENSE file for more information.
using System;
using System.Collections.Generic;
using System.Threading;
using ENet;
using IgnoranceThirdparty;
using UnityEngine;
using Event = ENet.Event;           // fixes CS0104 ambigous reference between the same thing in UnityEngine
using EventType = ENet.EventType;   // fixes CS0104 ambigous reference between the same thing in UnityEngine
using Object = System.Object;       // fixes CS0104 ambigous reference between the same thing in UnityEngine

namespace IgnoranceCore
{
    public class IgnoranceServer
    {
        // Server Properties
        // - Bind Settings
        public string BindAddress = "127.0.0.1";
        public int BindPort = 7777;
        // - Maximum allowed channels, peers, etc.
        public int MaximumChannels = 2;
        public int MaximumPeers = 100;
        public int MaximumPacketSize = 33554432;    // ENet.cs: uint maxPacketSize = 32 * 1024 * 1024 = 33554432
        // - Native poll waiting time
        public int PollTime = 1;
        // - Verbosity.
        public int Verbosity = 1;
        // - Queue Sizing
        public int IncomingOutgoingBufferSize = 5000;
        public int ConnectionEventBufferSize = 100;

        public bool IsAlive => WorkerThread != null && WorkerThread.IsAlive;

        private volatile bool CeaseOperation = false;

        // Queues
        // v1.4.0b9: Replace the queues with RingBuffers.
        public RingBuffer<IgnoranceIncomingPacket> Incoming;
        public RingBuffer<IgnoranceOutgoingPacket> Outgoing;
        public RingBuffer<IgnoranceCommandPacket> Commands;
        public RingBuffer<IgnoranceConnectionEvent> ConnectionEvents;
        public RingBuffer<IgnoranceConnectionEvent> DisconnectionEvents;
        public RingBuffer<IgnoranceServerStats> StatusUpdates;

        public RingBuffer<IgnoranceServerStats> RecycledServerStatBlocks = new RingBuffer<IgnoranceServerStats>(100);

        // Thread
        private Thread WorkerThread;

        public void Start()
        {
            if (WorkerThread != null && WorkerThread.IsAlive)
            {
                // Cannot do that.
                Debug.LogError("Ignorance Server: A worker thread is already running. Cannot start another.");
                return;
            }

            // Setup the ring buffers.
            SetupRingBuffersIfNull();

            CeaseOperation = false;
            ThreadParamInfo threadParams = new ThreadParamInfo()
            {
                Address = BindAddress,
                Port = BindPort,
                Peers = MaximumPeers,
                Channels = MaximumChannels,
                PollTime = PollTime,
                PacketSizeLimit = MaximumPacketSize,
                Verbosity = Verbosity
            };

            // Drain queues.
            if (Incoming != null) while (Incoming.TryDequeue(out _)) ;
            if (Outgoing != null) while (Outgoing.TryDequeue(out _)) ;
            if (Commands != null) while (Commands.TryDequeue(out _)) ;
            if (ConnectionEvents != null) while (ConnectionEvents.TryDequeue(out _)) ;
            if (DisconnectionEvents != null) while (DisconnectionEvents.TryDequeue(out _)) ;
            if (StatusUpdates != null) while (StatusUpdates.TryDequeue(out _)) ;

            WorkerThread = new Thread(ThreadWorker);
            WorkerThread.Start(threadParams);

            // Announce
            if (Verbosity > 0)
                Debug.Log("Ignorance Server: Dispatched worker thread.");
        }

        public void Stop()
        {
            // v1.4.0b7: Mirror may call this; if the worker thread isn't alive then don't announce it.
            if (WorkerThread != null && WorkerThread.IsAlive)
            {
                if (Verbosity > 0)
                    Debug.Log("Ignorance Server: Server stop acknowledged. Depending on network load, this may take a moment or two...");

                CeaseOperation = true;
            }
        }

        #region The meat and potatoes.
        private void ThreadWorker(Object parameters)
        {
            if (Verbosity > 0)
                Debug.Log("Ignorance Server: Initializing. Please stand by...");

            // Thread cache items
            ThreadParamInfo setupInfo;
            Address serverAddress = new Address();
            Host serverENetHost;
            Event serverENetEvent;

            Peer[] serverPeerArray;
            IgnoranceClientStats peerStats = default;

            // Grab the setup information.
            if (parameters.GetType() == typeof(ThreadParamInfo))
            {
                setupInfo = (ThreadParamInfo)parameters;
            }
            else
            {
                Debug.LogError("Ignorance Server: Startup failure; Invalid thread parameters. Aborting.");
                return;
            }

            // Attempt to initialize ENet inside the thread.
            if (Library.Initialize())
            {
                Debug.Log("Ignorance Server: ENet Native successfully initialized.");
            }
            else
            {
                Debug.LogError("Ignorance Server: Failed to initialize ENet Native. Aborting.");
                return;
            }

            // Configure the server address.
            serverAddress.SetHost(setupInfo.Address);
            serverAddress.Port = (ushort)setupInfo.Port;
            serverPeerArray = new Peer[setupInfo.Peers];

            using (serverENetHost = new Host())
            {
                // Create the server object.
                try
                {
                    serverENetHost.Create(serverAddress, setupInfo.Peers, setupInfo.Channels);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Ignorance Server: While attempting to create server host object, we caught an exception:\n{ex.Message}");
                    Debug.LogError($"If you are getting a \"Host creation call failed\" exception, please ensure you don't have a server already running on the same IP and Port.\n" +
                        $"Multiple server instances running on the same port are not supported. Also check to see if ports are not in-use by another application. In the worse case scenario, " +
                        $"restart your device to ensure no random background ENet threads are active that haven't been cleaned up correctly. If problems persist, please file a support ticket.");

                    Library.Deinitialize();
                    return;
                }

                // Loop until we're told to cease operations.
                while (!CeaseOperation)
                {
                    // Intermission: Command Handling
                    while (Commands.TryDequeue(out IgnoranceCommandPacket commandPacket))
                    {
                        switch (commandPacket.Type)
                        {
                            default:
                                break;

                            // Boot a Peer off the Server.
                            case IgnoranceCommandType.ServerKickPeer:
                                uint targetPeer = commandPacket.PeerId;

                                if (!serverPeerArray[targetPeer].IsSet) continue;

                                if (setupInfo.Verbosity > 0)
                                    Debug.Log($"Ignorance Server: Booting Peer {targetPeer} off");

                                IgnoranceConnectionEvent iced = new IgnoranceConnectionEvent
                                {
                                    EventType = 0x01,
                                    NativePeerId = targetPeer
                                };

                                DisconnectionEvents.Enqueue(iced);

                                // Disconnect and reset the peer array's entry for that peer.
                                serverPeerArray[targetPeer].DisconnectNow(0);
                                serverPeerArray[targetPeer] = default;
                                break;

                            case IgnoranceCommandType.ServerStatusRequest:
                                IgnoranceServerStats serverStats;
                                if (!RecycledServerStatBlocks.TryDequeue(out serverStats))
                                    serverStats.PeerStats = new Dictionary<int, IgnoranceClientStats>(setupInfo.Peers);

                                serverStats.PeerStats.Clear();

                                serverStats.BytesReceived = serverENetHost.BytesReceived;
                                serverStats.BytesSent = serverENetHost.BytesSent;

                                serverStats.PacketsReceived = serverENetHost.PacketsReceived;
                                serverStats.PacketsSent = serverENetHost.PacketsSent;

                                serverStats.PeersCount = serverENetHost.PeersCount;

                                for (int i = 0; i < serverPeerArray.Length; i++)
                                {
                                    if (!serverPeerArray[i].IsSet) continue;

                                    peerStats.RTT = serverPeerArray[i].RoundTripTime;

                                    peerStats.BytesReceived = serverPeerArray[i].BytesReceived;
                                    peerStats.BytesSent = serverPeerArray[i].BytesSent;

                                    peerStats.PacketsSent = serverPeerArray[i].PacketsSent;
                                    peerStats.PacketsLost = serverPeerArray[i].PacketsLost;

                                    serverStats.PeerStats.Add(i, peerStats);
                                }

                                StatusUpdates.Enqueue(serverStats);
                                break;
                        }
                    }

                    // Step One:
                    // ---> Sending to peers
                    while (Outgoing.TryDequeue(out IgnoranceOutgoingPacket outgoingPacket))
                    {
                        // Only create a packet if the server knows the peer.
                        if (serverPeerArray[outgoingPacket.NativePeerId].IsSet)
                        {
                            int ret = serverPeerArray[outgoingPacket.NativePeerId].Send(outgoingPacket.Channel, ref outgoingPacket.Payload);

                            if (ret < 0 && setupInfo.Verbosity > 0)
                                Debug.LogWarning($"Ignorance Server: ENet error {ret} while sending packet to Peer {outgoingPacket.NativePeerId}.");
                        }
                        else
                        {
                            // A peer might have disconnected, this is OK - just log the packet if set to paranoid.
                            if (setupInfo.Verbosity > 1)
                                Debug.LogWarning("Ignorance Server: Can't send packet, a native peer object is not set. This may be normal if the Peer has disconnected before this send cycle.");
                        }

                    }

                    // Step 2
                    // <--- Receiving from peers
                    bool pollComplete = false;

                    while (!pollComplete)
                    {
                        Packet incomingPacket;
                        Peer incomingPeer;
                        int incomingPacketLength;

                        // Any events happening?
                        if (serverENetHost.CheckEvents(out serverENetEvent) <= 0)
                        {
                            // If service time is met, break out of it.
                            if (serverENetHost.Service(setupInfo.PollTime, out serverENetEvent) <= 0) break;

                            pollComplete = true;
                        }

                        // Setup the packet references.
                        incomingPeer = serverENetEvent.Peer;

                        // What type are you?
                        switch (serverENetEvent.Type)
                        {
                            // Idle.
                            case EventType.None:
                            default:
                                break;

                            // Connection Event.
                            case EventType.Connect:
                                if (setupInfo.Verbosity > 1)
                                    Debug.Log($"Ignorance Server: Peer ID {incomingPeer.ID} says Hi.");

                                IgnoranceConnectionEvent ice = new IgnoranceConnectionEvent()
                                {
                                    NativePeerId = incomingPeer.ID,
                                    IP = incomingPeer.IP,
                                    Port = incomingPeer.Port
                                };

                                ConnectionEvents.Enqueue(ice);

                                // Assign a reference to the Peer.
                                serverPeerArray[incomingPeer.ID] = incomingPeer;
                                break;

                            // Disconnect/Timeout. Mirror doesn't care if it's either, so we lump them together.
                            case EventType.Disconnect:
                            case EventType.Timeout:
                                if (!serverPeerArray[incomingPeer.ID].IsSet) break;

                                if (setupInfo.Verbosity > 1)
                                    Debug.Log($"Ignorance Server: Peer {incomingPeer.ID} has disconnected.");

                                IgnoranceConnectionEvent iced = new IgnoranceConnectionEvent
                                {
                                    EventType = 0x01,
                                    NativePeerId = incomingPeer.ID
                                };

                                DisconnectionEvents.Enqueue(iced);

                                // Reset the peer array's entry for that peer.
                                serverPeerArray[incomingPeer.ID] = default;
                                break;

                            case EventType.Receive:
                                // Receive event type usually includes a packet; so cache its reference.
                                incomingPacket = serverENetEvent.Packet;
                                if (!incomingPacket.IsSet)
                                {
                                    if (setupInfo.Verbosity > 0)
                                        Debug.LogWarning($"Ignorance Server: A receive event did not supply us with a packet to work with. This should never happen.");
                                    break;
                                }

                                incomingPacketLength = incomingPacket.Length;

                                // Firstly check if the packet is too big. If it is, do not process it - drop it.
                                if (incomingPacketLength > setupInfo.PacketSizeLimit)
                                {
                                    if (setupInfo.Verbosity > 0)
                                        Debug.LogWarning($"Ignorance Server: Incoming packet is too big. My limit is {setupInfo.PacketSizeLimit} byte(s) whilest this packet is {incomingPacketLength} bytes.");

                                    incomingPacket.Dispose();
                                    break;
                                }

                                IgnoranceIncomingPacket incomingQueuePacket = new IgnoranceIncomingPacket
                                {
                                    Channel = serverENetEvent.ChannelID,
                                    NativePeerId = incomingPeer.ID,
                                    Payload = incomingPacket,
                                };

                                // Enqueue.
                                Incoming.Enqueue(incomingQueuePacket);
                                break;
                        }
                    }
                }

                if (Verbosity > 0)
                    Debug.Log("Ignorance Server: Thread shutdown commencing. Flushing connections.");

                // Cleanup and flush everything.
                serverENetHost.Flush();

                // Kick everyone.
                for (int i = 0; i < serverPeerArray.Length; i++)
                {
                    if (!serverPeerArray[i].IsSet) continue;
                    serverPeerArray[i].DisconnectNow(0);
                }
            }

            if (setupInfo.Verbosity > 0)
                Debug.Log("Ignorance Server: Shutdown complete.");

            Library.Deinitialize();
        }
        #endregion

        private void SetupRingBuffersIfNull()
        {
            Debug.Log($"Ignorance: Setting up ring buffers if they're not already created. " +
                $"If they are already, this step will be skipped.");

            if (Incoming == null)
                Incoming = new RingBuffer<IgnoranceIncomingPacket>(IncomingOutgoingBufferSize);
            if (Outgoing == null)
                Outgoing = new RingBuffer<IgnoranceOutgoingPacket>(IncomingOutgoingBufferSize);
            if (Commands == null)
                Commands = new RingBuffer<IgnoranceCommandPacket>(100);
            if (ConnectionEvents == null)
                ConnectionEvents = new RingBuffer<IgnoranceConnectionEvent>(ConnectionEventBufferSize);
            if (DisconnectionEvents == null)
                DisconnectionEvents = new RingBuffer<IgnoranceConnectionEvent>(ConnectionEventBufferSize);
            if (StatusUpdates == null)
                StatusUpdates = new RingBuffer<IgnoranceServerStats>(10);
        }

        private struct ThreadParamInfo
        {
            public int Channels;
            public int Peers;
            public int PollTime;
            public int Port;
            public int PacketSizeLimit;
            public int Verbosity;
            public string Address;
        }
    }
}
