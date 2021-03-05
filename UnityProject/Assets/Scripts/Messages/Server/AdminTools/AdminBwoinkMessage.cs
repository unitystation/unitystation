﻿using Mirror;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	public class AdminBwoinkMessage : ServerMessage<AdminBwoinkMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string AdminUID;
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			SoundManager.Play(SingletonSOSounds.Instance.Bwoink);
			Chat.AddAdminPrivMsg(msg.Message);
		}

		public static NetMessage  Send(GameObject recipient, string adminUid, string message)
		{
			NetMessage  msg = new NetMessage
			{
				AdminUID = adminUid,
				Message = message
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}