using Adrenak.UniVoice.Runtime.Types;
using Mirror;
using US13.Managers;

namespace US13.Messages.Client
{
	public class ClientVoiceData : ClientMessage<ClientVoiceData.UniVoiceMessage>
	{
		public struct UniVoiceMessage : NetworkMessage
		{
			public short audioSender;
			public string Tag;
			public short recipient;
			public ChatroomAudioSegment data;
		}

		public override void Process(UniVoiceMessage msg)
		{
			VoiceChatManager.Instance.Server_OnMessage(SentByPlayer.Connection, msg);
		}

		public static UniVoiceMessage Send( UniVoiceMessage msg)
		{
			NetworkClient.Send(msg, Mirror.Channels.Unreliable);
			return msg;
		}

	}
}