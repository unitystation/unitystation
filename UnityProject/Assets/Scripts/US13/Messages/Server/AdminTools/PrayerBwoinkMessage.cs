using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Managers;
using US13.Messages.Server.SoundMessages;
using US13.Player;

namespace US13.Messages.Server.AdminTools
{
	public class PrayerBwoinkMessage : ServerMessage<PrayerBwoinkMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string AdminUID;
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			_ = SoundManager.ClientPlayAtPosition(CommonSounds.Instance.Prayer, Vector3.zero, PlayerManager.LocalPlayerObject,
				null,
				false,
				true,
				audioSourceParameters: new AudioSourceParameters().MakeSoundGlobal().PitchVariation(0.05f));
			Chat.AddPrayerPrivMsg(msg.Message);
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
