using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using Communications;
using Managers;
using UnityEngine;
using Util;

namespace Objects.Telecomms
{
	public class LocalRadioListener : SignalEmitter
	{
		public void SendData(ChatEvent chat)
		{
			string chatMessage = chat.message;
			string chatSpeaker = chat.speaker;
			bool isEncrypted;
			if (signalData.EncryptionData != null)
			{
				chatMessage = EncryptionUtils.Encrypt(chatMessage, signalData.EncryptionData.EncryptionSecret);
				chatSpeaker = EncryptionUtils.Encrypt(chatSpeaker, signalData.EncryptionData.EncryptionSecret);
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
	}

}
