using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Logs;
using Debug = UnityEngine.Debug;

namespace Adrenak.UniVoice {
    /// <summary>
    /// Provides the means to host or connect to a chatroom.
    /// </summary>
    public class ChatroomAgent : IDisposable {
        const string TAG = "ChatroomAgent";

        // ====================================================================
        #region PROPERTIES
        // ====================================================================
        /// <summary>
        /// The underlying network which the agent uses to host or connect to
        /// chatrooms, and send and receive data to and from peers
        /// </summary>
        public IChatroomNetwork Network { get; private set; }

        /// <summary>
        /// Source of outgoing audio that can be
        /// transmitted over the network to peers
        /// </summary>
        public IAudioInput AudioInput { get; private set; }

        /// <summary>
        /// A factory that returns an <see cref="IAudioOutput"/>
        /// instance. Used every time a Peer connects for that peer to get
        /// an output for that peer.
        /// </summary>
        public IAudioOutputFactory AudioOutputFactory { get; private set; }

        /// <summary>
        /// There is a <see cref="IAudioOutput"/> for each peer that gets
        /// created using the provided <see cref="AudioOutputFactory"/>
        /// The <see cref="IAudioOutput"/> instance corresponding to a peer is
        /// responsible for playing the audio that we receive from that peer.
        /// </summary>
        public Dictionary<short, IAudioOutput> PeerOutputs;

        /// <summary>
        /// Fired when the <see cref="CurrentMode"/> changes.
        /// </summary>
        public Action<ChatroomAgentMode> OnModeChanged;

        /// <summary>
        /// The current <see cref="ChatroomAgentMode"/> of this agent
        /// </summary>
        public ChatroomAgentMode CurrentMode {
            get => _currentMode;
            private set {
                if(_currentMode != value) {
                    _currentMode = value;
                    OnModeChanged?.Invoke(value);
                    Loggy.Log(TAG + "Current Mode set to " + value);
                }
            }
        }
        ChatroomAgentMode _currentMode = ChatroomAgentMode.Unconnected;

        /// <summary>
        /// Mutes all the peers. If set to true, no incoming audio from other
        /// peers will be played. If you want to selectively mute a peer, use
        /// the <see cref="ChatroomPeerSettings.muteThem"/> flag in the
        /// <see cref="PeerSettings"/> instance for that peer.
        /// Note that setting this will not change <see cref="PeerSettings"/>
        /// </summary>
        public bool MuteOthers { get; set; }

        /// <summary>
        /// Whether this agent is muted or not. If set to true, voice data will
        /// not be sent to ANY peer. If you want to selectively mute yourself
        /// to a peer, use the <see cref="ChatroomPeerSettings.muteSelf"/>
        /// flag in the <see cref="PeerSettings"/> instance for that peer.
        /// Note that setting this will not change <see cref="PeerSettings"/>
        /// </summary>
        public bool MuteSelf { get; set; }

        /// <summary>
        /// <see cref="ChatroomPeerSettings"/> for each peer which allows you
        /// to read or change the settings for a specific peer. Use [id] to get
        /// settings for a peer with ID id;
        /// </summary>
        public Dictionary<short, ChatroomPeerSettings> PeerSettings;
        #endregion

        // ====================================================================
        #region CONSTRUCTION AND DISPOSAL
        // ====================================================================
        /// <summary>
        /// Creates and returns a new agent using the provided dependencies.
        /// The instance then makes the dependencies work together.
        /// </summary>
        ///
        /// <param name="chatroomNetwork">The chatroom network implementation
        /// for chatroom access and sending data to peers in a chatroom.
        /// </param>
        ///
        /// <param name="audioInput">The source of the outgoing audio</param>
        ///
        /// <param name="audioOutputFactory">
        /// The factory used for creating <see cref="IAudioOutput"/> instances
        /// for peers so that incoming audio from peers can be played.
        /// </param>
        public ChatroomAgent(
            IChatroomNetwork chatroomNetwork,
            IAudioInput audioInput,
            IAudioOutputFactory audioOutputFactory
        ) {
            AudioInput = audioInput ??
            throw new ArgumentNullException(nameof(audioInput));

            Network = chatroomNetwork ??
            throw new ArgumentNullException(nameof(chatroomNetwork));

            AudioOutputFactory = audioOutputFactory ??
            throw new ArgumentNullException(nameof(audioOutputFactory));

            CurrentMode = ChatroomAgentMode.Unconnected;
            MuteOthers = false;
            MuteSelf = false;
            PeerSettings = new Dictionary<short, ChatroomPeerSettings>();
            PeerOutputs = new Dictionary<short, IAudioOutput>();

            Loggy.Log(TAG + "Created");
            SetupEventListeners();
        }

        /// <summary>
        /// Disposes the instance. WARNING: Calling this method will
        /// also dispose the dependencies passed to it in the constructor.
        /// Be mindful of this if you're sharing dependencies between multiple
        /// instances and/or using them outside this instance.
        /// </summary>
        public void Dispose() {
	        Loggy.Log(TAG +  "Disposing");
            AudioInput.Dispose();

            RemoveAllPeers();
            PeerSettings.Clear();
            PeerOutputs.Clear();

            Network.Dispose();
            Loggy.Log(TAG +"Disposed");
        }
        #endregion

        // ====================================================================
        #region INTERNAL
        // ====================================================================
        void SetupEventListeners() {
	        Loggy.Log(TAG + "Setting up events.");

            // Network events
            Network.OnCreatedChatroom += () => {
                Loggy.Log(TAG + "Chatroom created.");
                CurrentMode = ChatroomAgentMode.Host;
            };
            Network.OnClosedChatroom += () => {
                Loggy.Log(TAG +  "Chatroom closed.");
                RemoveAllPeers();
                CurrentMode = ChatroomAgentMode.Unconnected;
            };
            Network.OnJoinedChatroom += id => {
                Loggy.Log(TAG +  "Joined chatroom.");
                CurrentMode = ChatroomAgentMode.Guest;
            };
            Network.OnLeftChatroom += () => {
                Loggy.Log(TAG +  "Left chatroom.");
                RemoveAllPeers();
                CurrentMode = ChatroomAgentMode.Unconnected;
            };
            Network.OnPeerJoinedChatroom += id => {
                Loggy.Log(TAG +  "New peer joined: " + id);
                AddPeer(id);
            };
            Network.OnPeerLeftChatroom += id => {
                Loggy.Log(TAG +  "Peer left: " + id);
                RemovePeer(id);
            };

            // Stream the incoming audio data using the right peer output
            Network.OnAudioReceived += (peerID, data, Object) => {
                // if we're muting all, do nothing.
                if (MuteOthers) return;

                if (AllowIncomingAudioFromPeer(peerID))
                {
	                PeerOutputs[peerID].Feed(data,Object);
                }

            };

            AudioInput.OnSegmentReady += (index, samples) => {
                if (CurrentMode == ChatroomAgentMode.Unconnected) return;

                // If we're muting ourselves to all, do nothing.
                if (MuteSelf) return;

                var ChannelCount = AudioInput.ChannelCount;
                if (ChannelCount == 0)
                {
	                ChannelCount = 1;
                }

                // Send the audio segment to every deserving recipient
                Network.SendAudioSegment(0, new ChatroomAudioSegment {
	                segmentIndex = index,
	                frequency = AudioInput.Frequency,
	                channelCount = ChannelCount,
	                samples = samples
                });

            };
            Loggy.Log(TAG + "Event setup completed.");
        }

        void AddPeer(short id) {
            // Ensure no old settings or outputs exist for this ID.
            RemovePeer(id);

            PeerSettings.Add(id, new ChatroomPeerSettings());

            var ChannelCount = AudioInput.ChannelCount;
            if (ChannelCount == 0)
            {
	            ChannelCount = 1;
            }

            var output = AudioOutputFactory.Create(
                AudioInput.Frequency,
                ChannelCount,
                AudioInput.Frequency * ChannelCount / AudioInput.SegmentRate
            );
            output.ID = id.ToString();
            PeerOutputs.Add(id, output);
            Loggy.Log(TAG +  "Added peer " + id);
        }

        void RemovePeer(short id) {
            if (PeerSettings.ContainsKey(id)) {
                PeerSettings.Remove(id);
                Loggy.Log(TAG + "Removed peer settings for ID " + id);
            }
            if (PeerOutputs.ContainsKey(id)) {
                PeerOutputs[id].Dispose();
                PeerOutputs.Remove(id);
                Loggy.Log(TAG + "Removed peer output for ID " + id);
            }
        }

        bool AllowIncomingAudioFromPeer(short id) {
            return PeerSettings.ContainsKey(id) && !PeerSettings[id].muteThem;
        }

        bool AllowOutgoingAudioToPeer(short id) {
            return PeerSettings.ContainsKey(id) && !PeerSettings[id].muteSelf;
        }

        void RemoveAllPeers() {
            Loggy.Log(TAG +  "Removing all peers");
            foreach(var peer in Network.PeerIDs)
                RemovePeer(peer);
        }
        #endregion
    }
}
