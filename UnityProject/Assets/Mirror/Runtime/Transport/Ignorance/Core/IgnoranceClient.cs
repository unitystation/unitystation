// Ignorance 1.4.x LTS (Long Term Support)
// https://github.com/SoftwareGuy/Ignorance
// -----------------
// Copyright (c) 2019 - 2021 Matt Coburn (SoftwareGuy/Coburn64)
// Ignorance is licensed under the MIT license. Refer
// to the LICENSE file for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using ENet;
using IgnoranceThirdparty;
using UnityEngine;
using Event = ENet.Event;           // fixes CS0104 ambigous reference between the same thing in UnityEngine
using EventType = ENet.EventType;   // fixes CS0104 ambigous reference between the same thing in UnityEngine
using Object = System.Object;       // fixes CS0104 ambigous reference between the same thing in UnityEngine

namespace IgnoranceCore
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
        // Maximum ring buffer capacity.
        public int IncomingOutgoingBufferSize = 5000;
        public int ConnectionEventBufferSize = 100;
        // Queues
        public RingBuffer<IgnoranceIncomingPacket> Incoming;
        public RingBuffer<IgnoranceOutgoingPacket> Outgoing;
        public RingBuffer<IgnoranceCommandPacket> Commands;
        public RingBuffer<IgnoranceConnectionEvent> ConnectionEvents;
        public RingBuffer<IgnoranceClientStats> StatusUpdates;

        public bool IsAlive => WorkerThread != null && WorkerThread.IsAlive;

        private volatile bool CeaseOperation = false;
        private Thread WorkerThread;

        public void Start()
        {
            if (WorkerThread != null && WorkerThread.IsAlive)
            {
                // Cannot do that.
                Debug.LogError("Ignorance Client: A worker thread is already running. Cannot start another.");
                return;
            }

            // Setup the ring buffers.
            SetupRingBuffersIfNull();

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

            Debug.Log("Ignorance Client: Dispatched worker thread.");
        }

        public void Stop()
        {
            if (WorkerThread != null && !CeaseOperation)
            {
                Debug.Log("Ignorance Client: Stop acknowledged. This may take a while depending on network load...");

                CeaseOperation = true;
            }
        }

        #region The meat and potatoes.
        // This runs in a seperate thread, be careful accessing anything outside of it's thread
        // or you may get an AccessViolation/crash.
        private void ThreadWorker(Object parameters)
        {
            if (Verbosity > 0)
                Debug.Log("Ignorance Client: Initializing. Please stand by...");

            ThreadParamInfo setupInfo;
            Address clientAddress = new Address();
            Peer clientPeer;        // The peer object that represents the client's connection.
            Host clientHost;        // NOT related to Mirror "Client Host". This is the client's ENet Host Object.
            Event clientEvent;      // Used when clients get events on the network.
            IgnoranceClientStats icsu = default;
            bool alreadyNotifiedAboutDisconnect = false;

            // Grab the setup information.
            if (parameters.GetType() == typeof(ThreadParamInfo))
                setupInfo = (ThreadParamInfo)parameters;
            else
            {
                Debug.LogError("Ignorance Client: Startup failure; Invalid thread parameters. Aborting.");
                return;
            }

            // Attempt to initialize ENet inside the thread.
            if (Library.Initialize())
                Debug.Log("Ignorance Client: ENet Native successfully initialized.");
            else
            {
                Debug.LogError("Ignorance Client: Failed to initialize ENet Native. Aborting.");
                return;
            }

            // Attempt to connect to our target.
            clientAddress.SetHost(setupInfo.Address);
            clientAddress.Port = (ushort)setupInfo.Port;

            using (clientHost = new Host())
            {
                try
                {
                    clientHost.Create();
                    clientPeer = clientHost.Connect(clientAddress, setupInfo.Channels);
                }
                catch (Exception ex)
                {
                    // Oops, something failed.
                    Debug.LogError($"Ignorance Client: Looks like something went wrong. While attempting to create client object, we caught an exception:\n{ex.Message}");
                    Debug.LogError($"You could try the debug-enabled version of the native ENet library which creates a logfile, or alternatively you could try restart " +
                        $"your device to ensure jank is cleared out of memory. If problems persist, please file a support ticket explaining what happened.");

                    Library.Deinitialize();
                    return;
                }

                // Process network events as long as we're not ceasing operation.
                while (!CeaseOperation)
                {
                    bool pollComplete = false;

                    while (Commands.TryDequeue(out IgnoranceCommandPacket ignoranceCommandPacket))
                    {
                        switch (ignoranceCommandPacket.Type)
                        {
                            case IgnoranceCommandType.ClientStatusRequest:
                                // Respond with statistics so far.
                                if (!clientPeer.IsSet)
                                    break;

                                icsu.RTT = clientPeer.RoundTripTime;

                                icsu.BytesReceived = clientPeer.BytesReceived;
                                icsu.BytesSent = clientPeer.BytesSent;

                                icsu.PacketsReceived = clientHost.PacketsReceived;
                                icsu.PacketsSent = clientPeer.PacketsSent;
                                icsu.PacketsLost = clientPeer.PacketsLost;

                                StatusUpdates.Enqueue(icsu);
                                break;

                            case IgnoranceCommandType.ClientWantsToStop:
                                CeaseOperation = true;
                                break;
                        }
                    }

                    // If something outside the thread has told us to stop execution, then we need to break out of this while loop.
                    if (CeaseOperation)
                        break;

                    // Step 1: Sending to Server
                    while (Outgoing.TryDequeue(out IgnoranceOutgoingPacket outgoingPacket))
                    {
                        // TODO: Revise this, could we tell the Peer to disconnect right here?                       
                        // Stop early if we get a client stop packet.
                        // if (outgoingPacket.Type == IgnorancePacketType.ClientWantsToStop) break;

                        int ret = clientPeer.Send(outgoingPacket.Channel, ref outgoingPacket.Payload);

                        if (ret < 0 && setupInfo.Verbosity > 0)
                            Debug.LogWarning($"Ignorance Client: ENet error {ret} while sending packet to Server via Peer {outgoingPacket.NativePeerId}.");
                    }

                    // If something outside the thread has told us to stop execution, then we need to break out of this while loop.
                    // while loop to break out of is while(!CeaseOperation).
                    if (CeaseOperation)
                        break;

                    // Step 2: Receive Data packets
                    // This loops until polling is completed. It may take a while, if it's
                    // a slow networking day.
                    while (!pollComplete)
                    {
                        Packet incomingPacket;
                        Peer incomingPeer;
                        int incomingPacketLength;

                        // Any events worth checking out?
                        if (clientHost.CheckEvents(out clientEvent) <= 0)
                        {
                            // If service time is met, break out of it.
                            if (clientHost.Service(setupInfo.PollTime, out clientEvent) <= 0) break;

                            // Poll is done.
                            pollComplete = true;
                        }

                        // Setup the packet references.
                        incomingPeer = clientEvent.Peer;

                        // Now, let's handle those events.
                        switch (clientEvent.Type)
                        {
                            case EventType.None:
                            default:
                                break;

                            case EventType.Connect:
                                if (setupInfo.Verbosity > 0)
                                    Debug.Log("Ignorance Client: ENet has connected to the server.");

                                ConnectionEvents.Enqueue(new IgnoranceConnectionEvent
                                {
                                    EventType = 0x00,
                                    NativePeerId = incomingPeer.ID,
                                    IP = incomingPeer.IP,
                                    Port = incomingPeer.Port
                                });
                                break;

                            case EventType.Disconnect:
                            case EventType.Timeout:
                                if (setupInfo.Verbosity > 0)
                                    Debug.Log("Ignorance Client: ENet has been disconnected from the server.");

                                ConnectionEvents.Enqueue(new IgnoranceConnectionEvent { EventType = 0x01 });
                                CeaseOperation = true;
                                alreadyNotifiedAboutDisconnect = true;
                                break;

                            case EventType.Receive:
                                // Receive event type usually includes a packet; so cache its reference.
                                incomingPacket = clientEvent.Packet;

                                if (!incomingPacket.IsSet)
                                {
                                    if (setupInfo.Verbosity > 0)
                                        Debug.LogWarning($"Ignorance Client: A receive event did not supply us with a packet to work with. This should never happen.");
                                    break;
                                }

                                incomingPacketLength = incomingPacket.Length;

                                // Never consume more than we can have capacity for.
                                if (incomingPacketLength > setupInfo.PacketSizeLimit)
                                {
                                    if (setupInfo.Verbosity > 0)
                                        Debug.LogWarning($"Ignorance Client: Incoming packet is too big. My limit is {setupInfo.PacketSizeLimit} byte(s) whilest this packet is {incomingPacketLength} bytes.");

                                    incomingPacket.Dispose();
                                    break;
                                }

                                IgnoranceIncomingPacket incomingQueuePacket = new IgnoranceIncomingPacket
                                {
                                    Channel = clientEvent.ChannelID,
                                    NativePeerId = incomingPeer.ID,
                                    Payload = incomingPacket
                                };

                                Incoming.Enqueue(incomingQueuePacket);
                                break;
                        }
                    }

                    // If something outside the thread has told us to stop execution, then we need to break out of this while loop.
                    // while loop to break out of is while(!CeaseOperation).
                    if (CeaseOperation)
                        break;
                }

                Debug.Log("Ignorance Client: Thread shutdown commencing. Disconnecting and flushing connection.");

                // Flush the client and disconnect.
                clientPeer.Disconnect(0);
                clientHost.Flush();

                // Fix for client stuck in limbo, since the disconnection event may not be fired until next loop.
                if (!alreadyNotifiedAboutDisconnect)
                {
                    ConnectionEvents.Enqueue(new IgnoranceConnectionEvent { EventType = 0x01 });
                    alreadyNotifiedAboutDisconnect = true;
                }

            }

            // Fix for client stuck in limbo, since the disconnection event may not be fired until next loop, again.
            if (!alreadyNotifiedAboutDisconnect)
                ConnectionEvents.Enqueue(new IgnoranceConnectionEvent { EventType = 0x01 });

            // Deinitialize
            Library.Deinitialize();

            if (setupInfo.Verbosity > 0)
                Debug.Log("Ignorance Client: Shutdown complete.");
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
            if (StatusUpdates == null)
                StatusUpdates = new RingBuffer<IgnoranceClientStats>(10);
        }

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
