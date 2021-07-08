// Ignorance 1.4.x
// Ignorance. It really kicks the Unity LLAPIs ass.
// https://github.com/SoftwareGuy/Ignorance
// -----------------
// Copyright (c) 2019 - 2020 Matt Coburn (SoftwareGuy/Coburn64)
// Ignorance Transport is licensed under the MIT license. Refer
// to the LICENSE file for more information.

using ENet;
// using NetStack.Buffers;
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using Event = ENet.Event;           // fixes CS0104 ambigous reference between the same thing in UnityEngine
using EventType = ENet.EventType;   // fixes CS0104 ambigous reference between the same thing in UnityEngine
using Object = System.Object;       // fixes CS0104 ambigous reference between the same thing in UnityEngine

namespace IgnoranceTransport
{
    public class IgnoranceClient
    {
        // Client connection address and port
        public string ConnectAddress = "127.0.0.1";
        public int ConnectPort = 7777;
        // How many channels are expected
        public int ExpectedChannels = 2;
        // Native poll waiting time
        public int PollTime = 1;
        // Maximum Packet Size
        public int MaximumPacketSize = 33554432;
        // General Verbosity by default.
        public int Verbosity = 1;

        // Queues
        public ConcurrentQueue<IgnoranceIncomingPacket> Incoming = new ConcurrentQueue<IgnoranceIncomingPacket>();
        public ConcurrentQueue<IgnoranceOutgoingPacket> Outgoing = new ConcurrentQueue<IgnoranceOutgoingPacket>();
        public ConcurrentQueue<IgnoranceCommandPacket> Commands = new ConcurrentQueue<IgnoranceCommandPacket>();
        public ConcurrentQueue<IgnoranceConnectionEvent> ConnectionEvents = new ConcurrentQueue<IgnoranceConnectionEvent>();
        public ConcurrentQueue<IgnoranceClientStats> StatusUpdates = new ConcurrentQueue<IgnoranceClientStats>();

        public bool IsAlive => WorkerThread != null && WorkerThread.IsAlive;

        private volatile bool CeaseOperation = false;
        private Thread WorkerThread;

        public void Start()
        {
            Debug.Log("IgnoranceClient.Start()");

            if (WorkerThread != null && WorkerThread.IsAlive)
            {
                // Cannot do that.
                Debug.LogError("A worker thread is already running. Cannot start another.");
                return;
            }

            CeaseOperation = false;
            ThreadParamInfo threadParams = new ThreadParamInfo()
            {
                Address = ConnectAddress,
                Port = ConnectPort,
                Channels = ExpectedChannels,
                PollTime = PollTime,
                PacketSizeLimit = MaximumPacketSize,
                Verbosity = Verbosity
            };

            // Drain queues.
            if (Incoming != null) while (Incoming.TryDequeue(out _)) ;
            if (Outgoing != null) while (Outgoing.TryDequeue(out _)) ;
            if (Commands != null) while (Commands.TryDequeue(out _)) ;
            if (ConnectionEvents != null) while (ConnectionEvents.TryDequeue(out _)) ;
            if (StatusUpdates != null) while (StatusUpdates.TryDequeue(out _)) ;

            WorkerThread = new Thread(ThreadWorker);
            WorkerThread.Start(threadParams);

            Debug.Log("Client has dispatched worker thread.");
        }

        public void Stop()
        {
            Debug.Log("Telling client thread to stop, this may take a while depending on network load");
            CeaseOperation = true;
        }

        // This runs in a seperate thread, be careful accessing anything outside of it's thread
        // or you may get an AccessViolation/crash.
        private void ThreadWorker(Object parameters)
        {
            if (Verbosity > 0)
                Debug.Log("Client Worker Thread: Startup");

            ThreadParamInfo setupInfo;
            Address clientAddress = new Address();
            Peer clientPeer;
            Host clientENetHost;
            Event clientENetEvent;
            IgnoranceClientStats icsu = default;

            // Grab the setup information.
            if (parameters.GetType() == typeof(ThreadParamInfo))
            {
                setupInfo = (ThreadParamInfo)parameters;
            }
            else
            {
                Debug.LogError("Thread worker startup failure: Invalid thread parameters. Aborting.");
                return;
            }

            // Attempt to initialize ENet inside the thread.
            if (Library.Initialize())
            {
                Debug.Log("Client Worker Thread: Initialized ENet.");
            }
            else
            {
                Debug.LogError("Client Worker Thread: Failed to initialize ENet. This threads' fucked.");
                return;
            }

            // Attempt to connect to our target.
            clientAddress.SetHost(setupInfo.Address);
            clientAddress.Port = (ushort)setupInfo.Port;

            using (clientENetHost = new Host())
            {
                // TODO: Maybe try catch this
                clientENetHost.Create();
                clientPeer = clientENetHost.Connect(clientAddress, setupInfo.Channels);

                while (!CeaseOperation)
                {
                    bool pollComplete = false;

                    // Step 0: Handle commands.
                    while (Commands.TryDequeue(out IgnoranceCommandPacket commandPacket))
                    {
                        switch (commandPacket.Type)
                        {
                            default:
                                break;

                            case IgnoranceCommandType.ClientWantsToStop:
                                CeaseOperation = true;
                                break;

                            case IgnoranceCommandType.ClientRequestsStatusUpdate:
                                // Respond with statistics so far.
                                if (!clientPeer.IsSet)
                                    break;

                                icsu.RTT = clientPeer.RoundTripTime;

                                icsu.BytesReceived = clientPeer.BytesReceived;
                                icsu.BytesSent = clientPeer.BytesSent;

                                icsu.PacketsReceived = clientENetHost.PacketsReceived;
                                icsu.PacketsSent = clientPeer.PacketsSent;
                                icsu.PacketsLost = clientPeer.PacketsLost;

                                StatusUpdates.Enqueue(icsu);
                                break;
                        }
                    }
                    // Step 1: Send out data.
                    // ---> Sending to Server
                    while (Outgoing.TryDequeue(out IgnoranceOutgoingPacket outgoingPacket))
                    {
                        // TODO: Revise this, could we tell the Peer to disconnect right here?                       
                        // Stop early if we get a client stop packet.
                        // if (outgoingPacket.Type == IgnorancePacketType.ClientWantsToStop) break;

                        int ret = clientPeer.Send(outgoingPacket.Channel, ref outgoingPacket.Payload);

                        if (ret < 0 && setupInfo.Verbosity > 0)
                            Debug.LogWarning($"Client Worker Thread: Failed sending a packet to Peer {outgoingPacket.NativePeerId}, error code {ret}");
                    }

                    // Step 2:
                    // <----- Receive Data packets
                    // This loops until polling is completed. It may take a while, if it's
                    // a slow networking day.
                    while (!pollComplete)
                    {
                        Packet incomingPacket;
                        Peer incomingPeer;
                        int incomingPacketLength;

                        // Any events worth checking out?
                        if (clientENetHost.CheckEvents(out clientENetEvent) <= 0)
                        {
                            // If service time is met, break out of it.
                            if (clientENetHost.Service(setupInfo.PollTime, out clientENetEvent) <= 0) break;

                            // Poll is done.
                            pollComplete = true;
                        }

                        // Setup the packet references.
                        incomingPeer = clientENetEvent.Peer;

                        // Now, let's handle those events.
                        switch (clientENetEvent.Type)
                        {
                            case EventType.None:
                            default:
                                break;

                            case EventType.Connect:
                                ConnectionEvents.Enqueue(new IgnoranceConnectionEvent()
                                {
                                    NativePeerId = incomingPeer.ID,
                                    IP = incomingPeer.IP,
                                    Port = incomingPeer.Port
                                });
                                break;

                            case EventType.Disconnect:
                            case EventType.Timeout:
                                ConnectionEvents.Enqueue(new IgnoranceConnectionEvent()
                                {
                                    WasDisconnect = true
                                });
                                break;


                            case EventType.Receive:
                                // Receive event type usually includes a packet; so cache its reference.
                                incomingPacket = clientENetEvent.Packet;

                                if (!incomingPacket.IsSet)
                                {
                                    if (setupInfo.Verbosity > 0)
                                        Debug.LogWarning($"Client Worker Thread: A receive event did not supply us with a packet to work with. This should never happen.");
                                    break;
                                }

                                incomingPacketLength = incomingPacket.Length;

                                // Never consume more than we can have capacity for.
                                if (incomingPacketLength > setupInfo.PacketSizeLimit)
                                {
                                    if (setupInfo.Verbosity > 0)
                                        Debug.LogWarning($"Client Worker Thread: Received a packet too large, {incomingPacketLength} bytes while our limit is {setupInfo.PacketSizeLimit} bytes.");

                                    incomingPacket.Dispose();
                                    break;
                                }

                                IgnoranceIncomingPacket incomingQueuePacket = new IgnoranceIncomingPacket
                                {
                                    Channel = clientENetEvent.ChannelID,
                                    NativePeerId = incomingPeer.ID,
                                    Payload = incomingPacket
                                };

                                Incoming.Enqueue(incomingQueuePacket);
                                break;
                        }
                    }
                }

                // Flush the client and disconnect.
                clientPeer.Disconnect(0);
                clientENetHost.Flush();
            }

            // Deinitialize
            Library.Deinitialize();
            if (setupInfo.Verbosity > 0)
                Debug.Log("Client Worker Thread: Shutdown.");
        }

        // TODO: Optimize struct layout.
        private struct ThreadParamInfo
        {
            public int Channels;
            public int PollTime;
            public int Port;
            public int PacketSizeLimit;
            public int Verbosity;
            public string Address;
        }
    }
}
