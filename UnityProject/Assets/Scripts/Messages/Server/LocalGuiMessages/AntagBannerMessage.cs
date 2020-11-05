using Antagonists;
using Audio.Managers;
using Mirror;
using UnityEngine;

namespace Messages.Server.LocalGuiMessages
{
	public class AntagBannerMessage: ServerMessage
	{
		public string AntagName;
		public string AntagSound;
		public Color TextColor;
		public Color BackgroundColor;
		private bool PlaySound;

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
				PlaySound = playSound
			};

			msg.SendTo(player);
			return msg;
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			writer.WriteString(AntagName);
			writer.WriteString(AntagSound);
			writer.WriteColor(TextColor);
			writer.WriteColor(BackgroundColor);
			writer.WriteBoolean(PlaySound);
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			AntagName = reader.ReadString();
			AntagSound = reader.ReadString();
			TextColor = reader.ReadColor();
			BackgroundColor = reader.ReadColor();
			PlaySound = reader.ReadBoolean();
		}

		public override void Process()
		{
			UIManager.Instance.antagBanner.Show(AntagName, TextColor, BackgroundColor);

			if (PlaySound)
			{
				// make sure that all sound is disabled
				SoundAmbientManager.StopAllAudio();
				//play the spawn sound
				SoundAmbientManager.PlayAudio(AntagSound);
			}
		}
	}
}