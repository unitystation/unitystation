using Audio.Managers;
using Mirror;
using UnityEngine;

namespace Messages.Server.LocalGuiMessages
{
	public class SpawnBannerMessage : ServerMessage<SpawnBannerMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Name;
			//AssetAddress
			public string SpawnSound;
			public Color TextColor;
			public Color BackgroundColor;
			public bool PlaySound;
		}

		public static NetMessage Send(
			GameObject player,
			string name,
			string spawnSound,
			Color textColor,
			Color backgroundColor,
			bool playSound)
		{
			NetMessage msg = new NetMessage
			{
				Name = name,
				SpawnSound = spawnSound,
				TextColor = textColor,
				BackgroundColor = backgroundColor,
				PlaySound = playSound
			};

			SendTo(player, msg);
			return msg;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.spawnBanner.Show(msg.Name, msg.TextColor, msg.BackgroundColor);

			if (msg.PlaySound)
			{
				// make sure that all sound is disabled
				SoundAmbientManager.StopAllAudio();
				//play the spawn sound
				SoundAmbientManager.PlayAudio(msg.SpawnSound);
			}
		}
	}
}
