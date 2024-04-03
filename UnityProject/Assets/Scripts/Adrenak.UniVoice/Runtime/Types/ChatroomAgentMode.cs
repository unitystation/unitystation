namespace Adrenak.UniVoice {
    [System.Serializable]
    /// <summary>
    /// Represents the mode that a <see cref="ChatroomAgent"/> is in.
    /// </summary>
    public enum ChatroomAgentMode {
        /// <summary>
        /// The agent is neither connected to a chatroom nor hosting one.
        /// </summary>
        Unconnected,

        /// <summary>
        /// The agent is hosting a chatroom. May or may not have guests
        /// </summary>
        Host,

        /// <summary>
        /// The agent has joined a chatroom and is a guest there
        /// </summary>
        Guest
    }
}