using Antagonists;
using UnityEngine;

namespace Messages.Server.LocalGuiMessages
{
	public class AntagBannerMessage: ServerMessage
	{
		public string AntagName;
		public string AntagSound;
		public Color TextColor;
		public Color BackgroundColor;
		private bool playSound;

		public static AntagBannerMessage Send(
			GameObject player,
			string antagName,
			string antagSound,
			Color textColor,
			Color backgroundColor,
			bool playSound)
		{
			AntagBannerMessage msg = new AntagBannerMessage
			{
				AntagName = antagName,
				AntagSound = antagSound,
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
				SoundManager.Play(AntagSound);
			}
		}
	}
}