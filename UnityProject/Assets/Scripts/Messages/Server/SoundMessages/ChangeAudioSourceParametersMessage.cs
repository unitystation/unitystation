using Mirror;
using UnityEngine;

namespace Messages.Server.SoundMessages
{
	/// <summary>
	/// Message that will change the Audio Source Parameters for a sound.
	/// </summary>
	public class ChangeAudioSourceParametersMessage : ServerMessage<ChangeAudioSourceParametersMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			// SoundSpawn Token to change Audio Source Parameters.
			public string SoundSpawnToken;

			// AudioSourceParameters to apply
			public AudioSourceParameters AudioSourceParameters;

			public override string ToString()
			{
				string audioSourceParametersValue = AudioSourceParameters.ToString();
				return $"{nameof(SoundSpawnToken)}: {SoundSpawnToken}, {nameof(AudioSourceParameters)}: {audioSourceParametersValue}";
			}
		}

		public override void Process(NetMessage msg)
		{
			SoundManager.ChangeAudioSourceParameters(msg.SoundSpawnToken, msg.AudioSourceParameters);
		}

		/// <summary>
		/// Send to a specific client to change the Audio Source Parameters of a playing sound
		/// </summary>
		/// <param name="recipient">The recipient of the message to be sent.</param>
		/// <param name="soundSpawnToken">The token that identifies the SoundSpawn uniquely among the server and all clients </param>
		/// <param name="audioSourceParameters">The Audio Source Parameters to apply.</param>
		/// <returns>The sent message</returns>
		public static NetMessage Send(GameObject recipient, string soundSpawnToken, AudioSourceParameters audioSourceParameters)
		{
			NetMessage msg = new NetMessage
			{
				SoundSpawnToken = soundSpawnToken,
				AudioSourceParameters = audioSourceParameters
			};

			SendTo(recipient, msg);
			return msg;
		}

		/// <summary>
		/// Send to all client to change the mixer of a playing sound
		/// </summary>
		/// <param name="soundSpawnToken">The token that identifies the SoundSpawn uniquely among the server and all clients </param>
		/// <param name="audioSourceParameters">The Audio Source Parameters to apply.</param>
		/// <returns>The sent message</returns>
		public static NetMessage SendToAll(string soundSpawnToken, AudioSourceParameters audioSourceParameters)
		{
			NetMessage msg = new NetMessage
			{
				SoundSpawnToken = soundSpawnToken,
				AudioSourceParameters = audioSourceParameters
			};

			SendToAll(msg);
			return msg;
		}
	}
}
