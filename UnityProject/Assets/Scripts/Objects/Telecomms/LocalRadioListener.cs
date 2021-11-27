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
		private ChatEvent chatEvent;

		public void SendData(ChatEvent @event)
		{
			chatEvent = @event;
			SendSignalLogic();
		}

		protected override bool SendSignalLogic()
		{
			if (chatEvent == null) return false;
			RadioMessage msg = new RadioMessage
			{
				Sender = chatEvent.speaker,
				Message = chatEvent.message,
			};
			TrySendSignal(msg);
			return true;
		}

		public override void SignalFailed()
		{
			Chat.AddLocalMsgToChat("ksshhhk!", gameObject);
		}
	}

}
