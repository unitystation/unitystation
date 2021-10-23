using Mirror;
using UnityEngine;

namespace Messages.Server.AdminTools
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
			_ = SoundManager.Play(CommonSounds.Instance.Bwoink);
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
