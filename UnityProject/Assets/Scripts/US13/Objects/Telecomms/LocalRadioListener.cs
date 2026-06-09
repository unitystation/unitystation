using Communications;
using US13.Core.Chat;
using US13.Managers;
using US13.Systems.Communications;

namespace US13.Objects.Telecomms
{
	public class LocalRadioListener : SignalEmitter, IChatInfluencer
	{
		public void SendData(ChatEvent chat)
		{
			string chatMessage = chat.message;
			string chatSpeaker = chat.speaker;
			int code = passCode;

			RadioMessage msg = new RadioMessage
			{
				Sender = chatSpeaker,
				Message = chatMessage,
				Code = code
			};
			TrySendSignal(null, msg);
		}

		protected override bool SendSignalLogic()
		{
			return true;
		}

		public override void SignalFailed()
		{
			Chat.AddActionMsgToChat(gameObject, "ksshhhk!");
		}

		public bool WillInfluenceChat()
		{
			return true; //other checks such as being powered can be checked for later
		}

		public ChatEvent InfluenceChat(ChatEvent chatToManipulate)
		{
			SendData(chatToManipulate);
			return chatToManipulate;
		}
	}

}
