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
		// SoundSpawn Token to change Audio Source Parameters.
		public string SoundSpawnToken;

		// AudioSourceParameters to apply
		public AudioSourceParameters AudioSourceParameters;
		
		public override void Process()
		{
			SoundManager.ChangeAudioSourceParameters(SoundSpawnToken, AudioSourceParameters);
		}

		/// <summary>
		/// Send to a specific client to change the Audio Source Parameters of a playing sound
		/// </summary>
		/// <param name="recipient">The recipient of the message to be sent.</param>
		/// <param name="soundSpawnToken">The token that identifies the SoundSpawn uniquely among the server and all clients </param>
		/// <param name="audioSourceParameters">The Audio Source Parameters to apply.</param>
		/// <returns>The sent message</returns>
		public static ChangeAudioSourceParametersMessage Send(GameObject recipient, string soundSpawnToken, AudioSourceParameters audioSourceParameters)
		{
			ChangeAudioSourceParametersMessage msg = new ChangeAudioSourceParametersMessage
			{
				SoundSpawnToken = soundSpawnToken,
				AudioSourceParameters = audioSourceParameters
			};

			msg.SendTo(recipient);
			return msg;
		}

		/// <summary>
		/// Send to all client to change the mixer of a playing sound
		/// </summary>
		/// <param name="soundSpawnToken">The token that identifies the SoundSpawn uniquely among the server and all clients </param>
		/// <param name="audioSourceParameters">The Audio Source Parameters to apply.</param>
		/// <returns>The sent message</returns>
		public static ChangeAudioSourceParametersMessage SendToAll(string soundSpawnToken, AudioSourceParameters audioSourceParameters)
		{
			ChangeAudioSourceParametersMessage msg = new ChangeAudioSourceParametersMessage
			{
				SoundSpawnToken = soundSpawnToken,
				AudioSourceParameters = audioSourceParameters
			};

			msg.SendToAll();

			return msg;
		}

		public override string ToString()
		{
			string audioSourceParametersValue = (AudioSourceParameters == null) ? "Null" : AudioSourceParameters.ToString();
			return $"{nameof(SoundSpawnToken)}: {SoundSpawnToken}, {nameof(AudioSourceParameters)}: {audioSourceParametersValue}";
		}
	}
}
