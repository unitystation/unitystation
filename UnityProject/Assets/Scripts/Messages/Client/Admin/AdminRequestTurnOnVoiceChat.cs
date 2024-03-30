using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Messages.Client.Admin
{
	public class AdminRequestTurnOnVoiceChat : ClientMessage<AdminRequestTurnOnVoiceChat.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool Enabled;
		}

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin())
			{
				VoiceChatManager.Instance.SyncEnabled(VoiceChatManager.Instance.Enabled, msg.Enabled);
			}
		}

		public static NetMessage Send(bool SetTo)
		{
			NetMessage msg = new()
			{
				Enabled = SetTo
			};

			Send(msg);
			return msg;
		}
	}
}
