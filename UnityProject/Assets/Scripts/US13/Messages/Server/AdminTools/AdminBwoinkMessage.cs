using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Managers;
using US13.Messages.Server.SoundMessages;

namespace US13.Messages.Server.AdminTools
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
			_ = SoundManager.Play(CommonSounds.Instance.Bwoink, audioSourceParameters : new AudioSourceParameters(spatialBlend: 1));
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
