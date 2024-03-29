
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;

using Mirror;

using Adrenak.BRW;
using Initialisation;
using Messages.Client;
using Messages.Server;

namespace Adrenak.UniVoice.MirrorNetwork {
    public class UniVoiceMirrorNetwork : IChatroomNetwork {
        // Packet tags
        const string NEW_CLIENT_INIT = "NEW_CLIENT_INIT";
        const string CLIENT_JOINED = "CLIENT_JOINED";
        const string CLIENT_LEFT = "CLIENT_LEFT";
        const string AUDIO_SEGMENT = "AUDIO_SEGMENT";

        // Hosting events
        public event Action OnCreatedChatroom;
        public event Action<Exception> OnChatroomCreationFailed;
        public event Action OnClosedChatroom;

        // Joining events
        public event Action<short> OnJoinedChatroom;
        public event Action<Exception> OnChatroomJoinFailed;
        public event Action OnLeftChatroom;

        // Peer events
        public event Action<short> OnPeerJoinedChatroom;
        public event Action<short> OnPeerLeftChatroom;

        // Audio events
        public event Action<short, ChatroomAudioSegment, uint> OnAudioReceived;
        public event Action<short, ChatroomAudioSegment> OnAudioSent;

        // Peer ID management
        public short OwnID { get; private set; } = -1;
        public List<short> PeerIDs { get; private set; } = new List<short>();

        // UniVoice peer ID <-> Mirror connection ID mapping
        short peerCount = 0;
        readonly Dictionary<short, int> clientMap = new Dictionary<short, int>();

        readonly UpdateHook updateHook;



        // Per frame code to detect change in NetworkManager mode
        NetworkManagerMode lastMode = NetworkManagerMode.Offline;
        public void OnUpdate() {
	        if (NetworkManager.singleton == null) return;
            var newMode = NetworkManager.singleton.mode;
            if(lastMode != newMode) {
                OnModeChanged(lastMode, newMode);
                lastMode = newMode;
            }
        }

        void OnModeChanged(NetworkManagerMode oldMode, NetworkManagerMode newMode) {
            // If we go from offline to host/server
            if(oldMode == NetworkManagerMode.Offline) {
                if (newMode == NetworkManagerMode.ServerOnly || newMode == NetworkManagerMode.Host)
                    OnCreatedChatroom?.Invoke();
            }
            // If we go from host/server to offline
            if(newMode == NetworkManagerMode.Offline) {
                if (oldMode == NetworkManagerMode.ServerOnly || oldMode == NetworkManagerMode.Host) {
                    OwnID = -1;
                    PeerIDs.Clear();
                    clientMap.Clear();
                    OnClosedChatroom?.Invoke();
                }
            }
        }

        public void SendAudioSegment(short recipientPeerId, ChatroomAudioSegment data) {
            if (IsOffline) return;

            // We write a packet with this data:
            // tag: string
            // senderPeerID: short
            // recipientPeerID: short
            // audio data: byte[]
            var packet = new BytesWriter()
                .WriteString(AUDIO_SEGMENT)
                .WriteShort(OwnID)
                .WriteShort(recipientPeerId)
                .WriteByteArray(ToByteArray(data));

            if (IsServerOrHost)
                SendToClient(recipientPeerId, packet.Bytes);
            else if(IsClient)
                SendToServer(packet.Bytes);

            OnAudioSent?.Invoke(recipientPeerId, data);
        }

        async void SendToClient(short peerID, byte[] bytes, int delay = 0, uint netID = NetId.Empty) {
            if (IsServerOrHost) {
	            if (delay != 0)
	            {
		            LoadManager.RegisterActionDelayed(() =>
		            {
			            ServerVoiceData.Send(new ServerVoiceData.UniVoiceMessage
			            {
				            recipient = peerID,
				            data = bytes,
				            Object = netID
			            });
		            }, delay);
	            }
	            else
	            {
		            ServerVoiceData.Send(new ServerVoiceData.UniVoiceMessage {
			            recipient = peerID,
			            data = bytes,
			            Object = netID
		            });
	            }


            }
        }

        void SendToServer(byte[] bytes)
        {
	        if (NetworkManager.singleton.mode == NetworkManagerMode.ClientOnly)
		        ClientVoiceData.Send(new ClientVoiceData.UniVoiceMessage
		        {
			        recipient = -1,
			        data = bytes
		        });
        }

        public void Client_OnConnected() {
            Debug.Log("Client connected to server. Awaiting initialization from server. " +
            "Connection ID : " + NetworkClient.connection.connectionId);
        }

        public void Client_OnDisconnected() {
            // If the client disconnects while own ID is -1, that means
            // it haven't connected earlier and the connection attempt has failed.
            if (OwnID == -1) {
                OnChatroomJoinFailed?.Invoke(new Exception("Could not join chatroom"));
                return;
            }

            // This method is *also* called on the server when the server is shutdown.
            // So we check peer ID to ensure that we're running this only on a peer.
            if (OwnID > 0) {
                OwnID = -1;
                PeerIDs.Clear();
                clientMap.Clear();
                OnLeftChatroom?.Invoke();
            }
        }

        public void Server_OnClientConnected(int connId) {
            // TODO: This causes the chatroom is to detected as created only when
            // the first peer joins. While this doesn't cause any bugs, it isn't right.
            if (IsServerOrHost && OwnID != 0) {
                OwnID = 0;
                OnCreatedChatroom?.Invoke();
            }

            // Connection ID 0 is the server connecting to itself with a client instance.
            // We do not need this.
            if (connId == 0) return;

            // We get a peer ID for this connection id
            var peerId = RegisterConnectionId(connId);

            // We go through each the peer that the server has registered
            foreach (var peer in PeerIDs) {
                // To the new peer, we send data to initialize it with.
                // This includes the following:
                // - peer Id: short: This tells the new peer its ID in the chatroom
                // - existing peers: short[]: This tells the new peer the IDs of the
                // peers that are already in the chatroom
                if (peer == peerId) {
                    // Get all the existing peer IDs except that of the newly joined peer
                    var existingPeersInitPacket = PeerIDs
                        .Where(x => x != peer)
                        .Select(x => (int)x)
                        .ToList();

                    // Server is ID 0, we add outselves to the peer list
                    // for the newly joined client
                    existingPeersInitPacket.Add(0);

                    var newClientPacket = new BytesWriter()
                        .WriteString(NEW_CLIENT_INIT)
                        .WriteInt(peerId)
                        .WriteIntArray(existingPeersInitPacket.ToArray());

                    // Server_OnClientConnected gets invoked as soon as a client connects
                    // to the server. But we use NetworkServer.SendToAll to send our packets
                    // and it seems the new Mirror Connection ID is not added to the KcpTransport
                    // immediately, so we send this with an artificial delay of 100ms.
                    SendToClient(-1, newClientPacket.Bytes, 1000);

                    string peerListString = string.Join(", ", existingPeersInitPacket);
                    Debug.Log($"Initializing new client with ID {peerId} and peer list {peerListString}");
                }
                // To the already existing peers, we let them know a new peer has joined
                else {
                    var newPeerNotifyPacked = new BytesWriter()
                        .WriteString(CLIENT_JOINED)
                        .WriteInt(peerId);
                    SendToClient(peer, newPeerNotifyPacked.Bytes);
                }
            }
            OnPeerJoinedChatroom?.Invoke(peerId);
        }

        public void Server_OnClientDisconnected(int connId) {
            // We use the peer map to get the peer ID for this connection ID
            var leftPeerId = GetPeerIdFromConnectionId(connId);

            // We now go ahead with the server handling a client leaving
            // Remove the peer from our peer list
            if (PeerIDs.Contains(leftPeerId))
                PeerIDs.Remove(leftPeerId);

            // Remove the peer-connection ID pair from the map
            if (clientMap.ContainsKey(leftPeerId))
                clientMap.Remove(leftPeerId);

            // Notify all remaining peers that a peer has left
            // so they can update their peer lists
            foreach (var peerId in PeerIDs) {
                var packet = new BytesWriter()
                    .WriteString(CLIENT_LEFT)
                    .WriteShort(leftPeerId);
                SendToClient(peerId, packet.Bytes);
            }
            OnPeerLeftChatroom?.Invoke(leftPeerId);
        }

        public void Client_OnMessage(ServerVoiceData.UniVoiceMessage message) {
            // The server can have a connection to itself, so we only process messages
            // on an instance that is the client.
            if (NetworkManager.singleton.mode != NetworkManagerMode.ClientOnly) return;
            if (OwnID == 0) return;

            // Unless we're the recipient of the message or the message is a broadcast
            // (recipient == -1), we don't process the message ahead.
            if (message.recipient == -1 && message.recipient != OwnID) return;

            try {
                var bytes = message.data;
                var packet = new BytesReader(bytes);
                var tag = packet.ReadString();

                switch (tag) {
                    // New client initialization has the following data (in this order):
                    // The peers ID: int
                    // The existing peers in the chatroom: int[]
                    case NEW_CLIENT_INIT:
                        // Get self ID and fire that joined chatroom event
                        OwnID = (short)packet.ReadInt();
                        OnJoinedChatroom?.Invoke(OwnID);

                        // Get the existing peer IDs from the message and fire
                        // the peer joined event for each of them
                        PeerIDs = packet.ReadIntArray().Select(x => (short)x).ToList();
                        PeerIDs.ForEach(x => OnPeerJoinedChatroom?.Invoke(x));

                        Debug.Log($"Initialized self with ID {OwnID} and peers {string.Join(", ", PeerIDs)}");
                        break;

                    // When a new peer joins, the existing peers add it to their state
                    // and fire the peer joined event
                    case CLIENT_JOINED:
                        var joinedID = (short)packet.ReadInt();
                        if (!PeerIDs.Contains(joinedID))
                            PeerIDs.Add(joinedID);
                        OnPeerJoinedChatroom?.Invoke(joinedID);
                        break;

                    // When a peer leaves, the existing peers remove it from their state
                    // and fire the peer left event
                    case CLIENT_LEFT:
                        var leftID = (short)packet.ReadInt();
                        if (PeerIDs.Contains(leftID))
                            PeerIDs.Remove(leftID);
                        OnPeerLeftChatroom?.Invoke(leftID);
                        break;

                    // When this peer receives audio, we find out the we we're the intended
                    // recipient of that audio segment. If so, we fire the audio received event.
                    // The data is as follows:
                    // sender: short
                    // recipient: short
                    // audio: byte[]
                    case AUDIO_SEGMENT:
                        var sender = packet.ReadShort();
                        var recepient = packet.ReadShort();
                        if (recepient == OwnID) {
                            var segment = FromByteArray<ChatroomAudioSegment>(packet.ReadByteArray());
                            OnAudioReceived?.Invoke(sender, segment, message.Object);
                        }
                        break;
                }
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }

        public void Server_OnMessage(NetworkConnectionToClient connection, ClientVoiceData.UniVoiceMessage message) {
            if (!IsServerOrHost) return;

            var bytes = message.data;
            var packet = new BytesReader(bytes);
            var tag = packet.ReadString();

            if (tag.Equals(AUDIO_SEGMENT)) {
                var audioSender = packet.ReadShort();
                var recipient = packet.ReadShort();
                var segmentBytes = packet.ReadByteArray();

                // If the audio is for the server, we invoke the audio received event.
                if (recipient == OwnID) {
	                var Info = PlayerList.Instance.GetOnline(connection);
                    var segment = FromByteArray<ChatroomAudioSegment>(segmentBytes);
                    OnAudioReceived?.Invoke(audioSender, segment , Info.GameObject.NetId());
                }
                // If the message is meant for someone else,
                // we forward it to the intended recipient.
                else if (PeerIDs.Contains(recipient))
                {
	                var Info = PlayerList.Instance.GetOnline(connection);
	                SendToClient(recipient, bytes, netID : Info.GameObject.NetId());
                }

            }
        }

        /// <summary>
        /// Returns the UniVoice peer Id corresponding to a previously
        /// registered Mirror connection Id
        /// </summary>
        /// <param name="connId">The connection Id to lookup</param>
        /// <returns>THe UniVoice Peer ID</returns>
        short GetPeerIdFromConnectionId(int connId) {
            foreach (var pair in clientMap) {
                if (pair.Value == connId)
                    return pair.Key;
            }
            return -1;
        }

        /// <summary>
        /// Connection ID need not be a short type. In Mirror, it can also be a very large
        /// number, for exmaple KcpTransport connection Ids can be something like 390231886
        /// Since UniVoice uses sequential short values to store peers, we generate a peer ID
        /// from any int connection Id and use a dictionary to store them in pairs.
        /// </summary>
        /// <param name="connId">The Mirror connection ID to be registered</param>
        /// <returns>The UniVoice Peer ID after registration</returns>
        short RegisterConnectionId(int connId) {
            peerCount++;
            clientMap.Add(peerCount, connId);
            PeerIDs.Add(peerCount);
            return peerCount;
        }

        bool IsServerOrHost {
            get {
                var mode = NetworkManager.singleton.mode;
                return mode == NetworkManagerMode.Host
                    || mode == NetworkManagerMode.ServerOnly;
            }
        }

        bool IsClient =>
            NetworkManager.singleton.mode == NetworkManagerMode.ClientOnly;

        bool IsOffline =>
            NetworkManager.singleton.mode == NetworkManagerMode.Offline;

        public byte[] ToByteArray<T>(T obj) {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream()) {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public T FromByteArray<T>(byte[] data) {
            if (data == null)
                return default;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(data)) {
                object obj = bf.Deserialize(ms);
                return (T)obj;
            }
        }

        public void HostChatroom(object data = null) {
            Debug.Log("HostChatroom is not supported. To host a chatroom, start a Mirror server.");
        }

        public void CloseChatroom(object data = null) {
            Debug.Log("CloseChatroom is not supported. To close a chatroom, stop your Mirror server.");
        }

        public void JoinChatroom(object data = null) {
            Debug.Log("JoinChatroom is not supported. To join a  chatroom, connect to a Mirror server.");
        }

        public void LeaveChatroom(object data = null) {
            Debug.Log("LeaveChatroom is not supported. To leave the chatroom, disconnect from your Mirror server.");
        }

        public void Dispose() {

        }
    }
}
