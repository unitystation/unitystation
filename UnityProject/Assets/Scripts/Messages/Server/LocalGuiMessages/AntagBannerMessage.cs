using AddressableReferences;
using Antagonists;
using UnityEngine;

namespace Messages.Server.LocalGuiMessages
{
	public class AntagBannerMessage: ServerMessage
	{
		public string AntagName;
		public string AntagSoundGuid;
		public Color TextColor;
		public Color BackgroundColor;
		private bool playSound;

		public static AntagBannerMessage Send(
			GameObject player,
			string antagName,
			AddressableAudioSource antagSound,
			Color textColor,
			Color backgroundColor,
			bool playSound)
		{
			AntagBannerMessage msg = new AntagBannerMessage
			{
				AntagName = antagName,
				AntagSoundGuid = antagSound.AssetReference.AssetGUID,
				TextColor = textColor,
				BackgroundColor = backgroundColor,
				playSound = playSound
			};

			msg.SendTo(player);
			return msg;
		}


		public override void Process()
		{
			UIManager.Instance.antagBanner.Show(AntagName, TextColor, BackgroundColor);

			if (playSound)
			{
				// Recompose an AddressableAudioSoure from its primart key (Guid)
				AddressableAudioSource addressableAudioSource = new AddressableAudioSource(AntagSoundGuid);
				SoundManager.Play(addressableAudioSource, string.Empty);
			}
		}
	}
}