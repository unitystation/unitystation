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
			public ushort LanguageId;
		}

		public override void Process(NetMessage msg)
		{
			if (SentByPlayer != PlayerInfo.Invalid)
			{
				Chat.AddChatMsgToChatServer(SentByPlayer, msg.ChatMessageText, msg.Channels, msg.Loudness, msg.LanguageId);
			}
		}

		//This is only used to send the chat input on the client to the server
		public static NetMessage Send(string message, ChatChannel channels, Loudness loudness = Loudness.NORMAL, ushort languageId = 0)
		{
			NetMessage msg = new NetMessage
			{
				Channels = channels,
				ChatMessageText = message,
				Loudness = loudness,
				LanguageId = languageId
			};

			Send(msg);
			return msg;
		}
	}
}
