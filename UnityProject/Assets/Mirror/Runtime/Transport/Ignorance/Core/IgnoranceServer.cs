// Ignorance 1.4.x
// https://github.com/SoftwareGuy/Ignorance
// -----------------
// Copyright (c) 2019 - 2021 Matt Coburn (SoftwareGuy/Coburn64)
// Ignorance Transport is licensed under the MIT license. Refer
// to the LICENSE file for more information.
using System.Collections.Concurrent;
using System.Threading;
using ENet;
using UnityEngine;
using Event = ENet.Event;           // fixes CS0104 ambigous reference between the same thing in UnityEngine
using EventType = ENet.EventType;   // fixes CS0104 ambigous reference between the same thing in UnityEngine
using Object = System.Object;       // fixes CS0104 ambigous reference between the same thing in UnityEngine

namespace IgnoranceTransport
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
        public int Verbosity = 1;

        public bool IsAlive => WorkerThread != null && WorkerThread.IsAlive;

        private volatile bool CeaseOperation = false;

        // Queues
        public ConcurrentQueue<IgnoranceIncomingPacket> Incoming = new ConcurrentQueue<IgnoranceIncomingPacket>();
        public ConcurrentQueue<IgnoranceOutgoingPacket> Outgoing = new ConcurrentQueue<IgnoranceOutgoingPacket>();
        public ConcurrentQueue<IgnoranceCommandPacket> Commands = new ConcurrentQueue<IgnoranceCommandPacket>();
        public ConcurrentQueue<IgnoranceConnectionEvent> ConnectionEvents = new ConcurrentQueue<IgnoranceConnectionEvent>();
        public ConcurrentQueue<IgnoranceConnectionEvent> DisconnectionEvents = new ConcurrentQueue<IgnoranceConnectionEvent>();

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
                Debug.LogError("Ignorance Server: Failed to initialize ENet Native. This threads' fucked.");
                return;
            }

            // Configure the server address.
            serverAddress.SetHost(setupInfo.Address);
            serverAddress.Port = (ushort)setupInfo.Port;
            serverPeerArray = new Peer[setupInfo.Peers];

            using (serverENetHost = new Host())
            {
                // Create the server object.
                serverENetHost.Create(serverAddress, setupInfo.Peers, setupInfo.Channels);

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
                                    Debug.Log($"Ignorance Server: Booting ENet Peer {targetPeer} off this server instance.");

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
                                    Debug.Log($"Ignorance Server: Hello new peer with ID {incomingPeer.ID}!");

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
                                    Debug.Log($"Ignorance Server: Bye bye Peer {incomingPeer.ID}; They have disconnected.");

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
