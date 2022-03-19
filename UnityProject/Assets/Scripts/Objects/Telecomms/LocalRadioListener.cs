using Communications;
using Managers;
using Systems.Communications;
using Util;

namespace Objects.Telecomms
{
	public class LocalRadioListener : SignalEmitter, IChatInfluncer
	{
		public void SendData(ChatEvent chat)
		{
			string chatMessage = chat.message;
			string chatSpeaker = chat.speaker;
			bool isEncrypted;
			if (EncryptionData != null)
			{
				chatMessage = EncryptionUtils.Encrypt(chatMessage, EncryptionData.EncryptionSecret);
				chatSpeaker = EncryptionUtils.Encrypt(chatSpeaker, EncryptionData.EncryptionSecret);
				isEncrypted = true;
			}
			else
			{
				isEncrypted = false;
			}
			RadioMessage msg = new RadioMessage
			{
				Sender = chatSpeaker,
				Message = chatMessage,
				IsEncrypted = isEncrypted,
				OriginalSenderName = chat.speaker
			};
			TrySendSignal(msg);
		}

		protected override bool SendSignalLogic()
		{
			return true;
		}

		public override void SignalFailed()
		{
			Chat.AddLocalMsgToChat("ksshhhk!", gameObject);
		}

		public bool RunChecks()
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
