using Audio.Managers;
using Mirror;
using UnityEngine;

namespace Messages.Server.LocalGuiMessages
{
	public class SpawnBannerMessage : ServerMessage
	{
		public class SpawnBannerMessageNetMessage : NetworkMessage
		{
			public string Name;
			//AssetAddress
			public string SpawnSound;
			public Color TextColor;
			public Color BackgroundColor;
			public bool PlaySound;
		}

		public static SpawnBannerMessageNetMessage Send(
			GameObject player,
			string name,
			string spawnSound,
			Color textColor,
			Color backgroundColor,
			bool playSound)
		{
			SpawnBannerMessageNetMessage msg = new SpawnBannerMessageNetMessage
			{
				Name = name,
				SpawnSound = spawnSound,
				TextColor = textColor,
				BackgroundColor = backgroundColor,
				PlaySound = playSound
			};

			new SpawnBannerMessage().SendTo(player, msg);
			return msg;
		}

		public override void Process<T>(T msg)
		{
			var newMsgNull = msg as SpawnBannerMessageNetMessage?;
			if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

			UIManager.Instance.spawnBanner.Show(newMsg.Name, newMsg.TextColor, newMsg.BackgroundColor);

			if (newMsg.PlaySound)
			{
				// make sure that all sound is disabled
				SoundAmbientManager.StopAllAudio();
				//play the spawn sound
				SoundAmbientManager.PlayAudio(newMsg.SpawnSound);
			}
		}
	}
}
