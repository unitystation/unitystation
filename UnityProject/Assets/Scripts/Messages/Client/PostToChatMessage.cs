using Mirror;

namespace Messages.Client
{
	/// <summary>
	///     Attempts to send a chat message to the server
	/// </summary>
	public class PostToChatMessage: ClientMessage<PostToChatMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ChatChannel Channels;
			public string ChatMessageText;
			public Loudness Loudness;
		}

		public override void Process(NetMessage msg)
		{
			if (SentByPlayer != ConnectedPlayer.Invalid)
			{
				Chat.AddChatMsgToChat(SentByPlayer, msg.ChatMessageText, msg.Channels, msg.Loudness);
			}
		}

		//This is only used to send the chat input on the client to the server
		public static NetMessage Send(string message, ChatChannel channels, Loudness loudness = Loudness.NORMAL)
		{
			NetMessage msg = new NetMessage
			{
				Channels = channels,
				ChatMessageText = message,
				Loudness = loudness
			};

			Send(msg);
			return msg;
		}
	}
}
