using Messages.Client;
using Messages.Server.SoundMessages;
using Mirror;
using UnityEngine;

namespace Messages.Client.Admin
{
	public class AdminReplyMessage : ClientMessage<AdminReplyMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.adminChatWindows.adminPlayerChat.ServerAddChatRecord(msg.Message, SentByPlayer);
			PlaySoundMessage.SendToAdmins(CommonSounds.Instance.Bwoink, Vector3.zero, false,
				null,
				default,
				new AudioSourceParameters().MakeSoundGlobal().PitchVariation(0.05f));
		}

		public static NetMessage Send(string message)
		{
			NetMessage msg = new NetMessage
			{
				Message = message
			};

			Send(msg);
			return msg;
		}
	}
}
