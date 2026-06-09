using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Messages.Server.SoundMessages;
using US13.UI.Systems;

namespace US13.Messages.Client.Admin
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
