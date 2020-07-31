using UnityEngine;

namespace Assets.Scripts.Messages.Server.SoundMessages
{
	/// <summary>
	/// Message that will change the sound Mixer on a sound currently playing
	/// </summary>
	public class ChangeMixerMessage : ServerMessage
	{
		// Name of the sound to change mixer.
		public string SoundName;

		// Name of the mixer to apply
		public string MixerName;

		public override void Process()
		{
			SoundManager.ChangeMixer(SoundName, MixerName);
		}

		/// <summary>
		/// Send to a specific client to change the mixer of a playing sound
		/// </summary>
		/// <param name="recipient">The recipient of the message to be sent.</param>
		/// <param name="soundName">The name of the sound.</param>
		/// <param name="mixerName">The name of the new mixer.</param>
		/// <returns>The sent message</returns>
		public static ChangeMixerMessage Send(GameObject recipient, string soundName, string mixerName)
		{
			ChangeMixerMessage msg = new ChangeMixerMessage
			{
				SoundName = soundName,
				MixerName = mixerName
			};

			msg.SendTo(recipient);
			return msg;
		}

		/// <summary>
		/// Send to all client to change the mixer of a playing sound
		/// </summary>
		/// <param name="soundName">The name of the sound.</param>
		/// <param name="mixerName">The name of the new mixer.</param>
		/// <returns>The sent message</returns>
		public static ChangeMixerMessage SendToAll(string soundName, string mixerName)
		{
			ChangeMixerMessage msg = new ChangeMixerMessage
			{
				SoundName = soundName,
				MixerName = mixerName
			};

			msg.SendToAll();

			return msg;
		}
	}
}
