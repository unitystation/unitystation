using System;
using System.Collections.Generic;

namespace Adrenak.UniVoice {
    /// <summary>
    /// A chatroom specific networking interface for creating & joining
    /// chatrooms and sending & receiving data to and from chatroom peers.
    /// </summary>
    public interface IChatroomNetwork : IDisposable {
        // ====================================================================
        #region EVENTS
        // ====================================================================
        /// <summary>
        /// Fired when a chatroom is created.
        /// </summary>
        event Action OnCreatedChatroom;

        /// <summary>
        /// Fired when the attempt to create a chatroom fails.
        /// Provides an exception as event data.
        /// </summary>
        event Action<Exception> OnChatroomCreationFailed;

        /// <summary>
        /// Fired when a chatroom is closed.
        /// </summary>
        event Action OnClosedChatroom;

        /// <summary>
        /// Fired when the local user joins a chatroom.
        /// Provides the chatroom ID assigned as event data.
        /// </summary>
        event Action<short> OnJoinedChatroom;

        /// <summary>
        /// Fired when an attempt to join a chatroom fails.
        /// Provides an exception as event data.
        /// </summary>
        event Action<Exception> OnChatroomJoinFailed;

        /// <summary>
        /// Fired when the local user leaves a chatroom
        /// </summary>
        event Action OnLeftChatroom;

        /// <summary>
        /// Fired when a peer joins the chatroom.
        /// Provides the ID of the peer as event data.
        /// NOTE: This action also MUST be called for all previously
        /// existing peers when a local user connects to a network.
        /// This allows the local user to know about the users that
        /// were in the chatroom before they joined.
        /// </summary>
        event Action<short> OnPeerJoinedChatroom;

        /// <summary>
        /// Fired when a peer leaves the chatroom.
        /// Provides the ID of the peer as event data.
        /// </summary>
        event Action<short> OnPeerLeftChatroom;

        /// <summary>
        /// Fired when the network receives audio data from a peer.
        /// The first argument is the ID of the user the audio came from.
        /// The second is the audio segment.
        /// </summary>
        event Action<short, ChatroomAudioSegment, uint> OnAudioReceived;

        /// <summary>
        /// Fired when the local user sets audio data to a peer.
        /// The first argument is the ID of the user the audio was sent to.
        /// The second is the audio segment.
        /// </summary>
        event Action<short, ChatroomAudioSegment> OnAudioSent;
        #endregion

        // ====================================================================
        #region PROPERTIES
        // ====================================================================
        /// <summary>
        /// The ID of the local user in the current chatroom
        /// </summary>
        short OwnID { get; }

        /// <summary>
        /// IDs of all the peers in the current chatroom (excluding <see cref="OwnID"/>)
        /// </summary>
        List<short> PeerIDs { get; }
        #endregion

        // ====================================================================
        #region METHODS
        // ====================================================================
        /// <summary>
        /// Creates a chatroom
        /// </summary>
        /// <param name="data">Any arguments for hosting a chatroom</param>
        void HostChatroom(object data = null);

        /// <summary>
        /// Closes a chatroom that the local user is hosting
        /// </summary>
        /// <param name="data">Any arguments used for closing the chatroom</param>
        void CloseChatroom(object data = null);

        /// <summary>
        /// Joins a chatroom
        /// </summary>
        /// <param name="data">Any arguments used to join a chatroom</param>
        void JoinChatroom(object data = null);

        /// <summary>
        /// Leaves the chatroom the local user is currently in, if any
        /// </summary>
        /// <param name="data">Any arguments used to leave a chatroom</param>
        void LeaveChatroom(object data = null);

        /// <summary>
        /// Sends audio data over the network
        /// </summary>
        /// <param name="data">The data to be transmitted.</param>
        void SendAudioSegment(short peerID, ChatroomAudioSegment data);
        #endregion
    }
}
