using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Managers;
using US13.Messages.Server.SoundMessages;

namespace US13.Messages.Server.AdminTools
{
	public class MentorBwoinkMessage : ServerMessage<MentorBwoinkMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string MentorUID;
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			_ = SoundManager.Play(CommonSounds.Instance.Bwoink, audioSourceParameters : new AudioSourceParameters(spatialBlend: 1));
			Chat.AddMentorPrivMsg(msg.Message);
		}

		public static NetMessage  Send(GameObject recipient, string mentorUid, string message)
		{
			NetMessage  msg = new NetMessage
			{
				MentorUID = mentorUid,
				Message = message
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
