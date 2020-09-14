using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts.Messages.Server.SoundMessages
{
	/// <summary>
	/// Message that will change the Audio Source Parameters for a sound.
	/// </summary>
	public class ChangeAudioSourceParametersMessage : ServerMessage
	{
		// Name of the sound to change Audio Source Parameters.
		public string SoundGuid;

		// AudioSourceParameters to apply
		public AudioSourceParameters AudioSourceParameters;
		
		public override void Process()
		{
			SoundManager.ChangeAudioSourceParameters(SoundGuid, AudioSourceParameters);
		}

		/// <summary>
		/// Send to a specific client to change the Audio Source Parameters of a playing sound
		/// </summary>
		/// <param name="recipient">The recipient of the message to be sent.</param>
		/// <param name="soundGuid">The guid of the sound.</param>
		/// <param name="audioSourceParameters">The Audio Source Parameters to apply.</param>
		/// <returns>The sent message</returns>
		public static ChangeAudioSourceParametersMessage Send(GameObject recipient, string soundGuid, AudioSourceParameters audioSourceParameters)
		{
			ChangeAudioSourceParametersMessage msg = new ChangeAudioSourceParametersMessage
			{
				SoundGuid = soundGuid,
				AudioSourceParameters = audioSourceParameters
			};

			msg.SendTo(recipient);
			return msg;
		}

		/// <summary>
		/// Send to all client to change the mixer of a playing sound
		/// </summary>
		/// <param name="soundGuid">The name of the sound.</param>
		/// <param name="audioSourceParameters">The Audio Source Parameters to apply.</param>
		/// <returns>The sent message</returns>
		public static ChangeAudioSourceParametersMessage SendToAll(string soundGuid, AudioSourceParameters audioSourceParameters)
		{
			ChangeAudioSourceParametersMessage msg = new ChangeAudioSourceParametersMessage
			{
				SoundGuid = soundGuid,
				AudioSourceParameters = audioSourceParameters
			};

			msg.SendToAll();

			return msg;
		}

		public override string ToString()
		{
			string audioSourceParametersValue = (AudioSourceParameters == null) ? "Null" : AudioSourceParameters.ToString();
			return $"{nameof(SoundGuid)}: {SoundGuid}, {nameof(AudioSourceParameters)}: {audioSourceParametersValue}";
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.WriteString(SoundGuid);
			writer.WriteString(JsonConvert.SerializeObject(AudioSourceParameters));
		}

		public override void Deserialize(NetworkReader reader)
		{
			SoundGuid = reader.ReadString();
			AudioSourceParameters = JsonConvert.DeserializeObject<AudioSourceParameters>(reader.ReadString());
		}
	}
}
