namespace Adrenak.UniVoice {
    [System.Serializable]
    /// <summary>
    /// Represents settings associated with a peer in the chatroom
    /// </summary>
    public class ChatroomPeerSettings {
        /// <summary>
        /// Whether this peer is muted. Use this to ignore a person.
        /// </summary>
        public bool muteThem = false;

        /// <summary>
        /// Whether this peer will receive out voice. Use this to 
        /// stop sending your audio to a peer.
        /// </summary>
        public bool muteSelf = false;
    }
}