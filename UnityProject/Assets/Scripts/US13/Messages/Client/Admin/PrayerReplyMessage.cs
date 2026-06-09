
using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Messages.Server.SoundMessages;
using US13.UI.Systems;

namespace US13.Messages.Client.Admin
{
	public class PrayerReplyMessage : ClientMessage<PrayerReplyMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			PlaySoundMessage.SendToAdmins(CommonSounds.Instance.Prayer, Vector3.zero, false,
				null,
				default,
				new AudioSourceParameters().MakeSoundGlobal().PitchVariation(0.05f));
			UIManager.Instance.adminChatWindows.playerPrayerWindow.ServerAddChatRecord(msg.Message, SentByPlayer);
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
