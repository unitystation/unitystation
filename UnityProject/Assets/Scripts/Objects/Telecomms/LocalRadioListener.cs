using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using Communications;
using Managers;
using UnityEngine;

namespace Objects.Telecomms
{
	public class LocalRadioListener : SignalEmitter
	{
		public void SendData(ChatEvent chat)
		{
			RadioMessage msg = new RadioMessage
			{
				Sender = chat.speaker,
				Message = chat.message,
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
