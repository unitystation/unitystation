using Audio.Managers;
using UnityEngine;

namespace Messages.Server.LocalGuiMessages
{
	public class SpawnBannerMessage: ServerMessage
	{
		public string Name;
		//AssetAddress
		public string SpawnSound;
		public Color TextColor;
		public Color BackgroundColor;
		public bool PlaySound;

		public static SpawnBannerMessage Send(
			GameObject player,
			string name,
			string spawnSound,
			Color textColor,
			Color backgroundColor,
			bool playSound)
		{
			SpawnBannerMessage msg = new SpawnBannerMessage
			{
				Name = name,
				SpawnSound = spawnSound,
				TextColor = textColor,
				BackgroundColor = backgroundColor,
				PlaySound = playSound
			};

			msg.SendTo(player);
			return msg;
		}

		public override void Process()
		{
			UIManager.Instance.spawnBanner.Show(Name, TextColor, BackgroundColor);

			if (PlaySound)
			{
				// make sure that all sound is disabled
				SoundAmbientManager.StopAllAudio();
				//play the spawn sound
				SoundAmbientManager.PlayAudio(SpawnSound);
			}
		}
	}
}
