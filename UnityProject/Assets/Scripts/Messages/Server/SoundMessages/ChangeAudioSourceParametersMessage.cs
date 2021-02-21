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
		public struct ChangeAudioSourceParametersMessageNetMessage : NetworkMessage
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

		//This is needed so the message can be discovered in NetworkManagerExtensions
		public ChangeAudioSourceParametersMessageNetMessage message;

		public override void Process<T>(T msg)
		{
			var newMsgNull = msg as ChangeAudioSourceParametersMessageNetMessage?;
			if(newMsgNull == null) return;
			var newMsg = newMsgNull.Value;

			SoundManager.ChangeAudioSourceParameters(newMsg.SoundSpawnToken, newMsg.AudioSourceParameters);
		}

		/// <summary>
		/// Send to a specific client to change the Audio Source Parameters of a playing sound
		/// </summary>
		/// <param name="recipient">The recipient of the message to be sent.</param>
		/// <param name="soundSpawnToken">The token that identifies the SoundSpawn uniquely among the server and all clients </param>
		/// <param name="audioSourceParameters">The Audio Source Parameters to apply.</param>
		/// <returns>The sent message</returns>
		public static ChangeAudioSourceParametersMessageNetMessage Send(GameObject recipient, string soundSpawnToken, AudioSourceParameters audioSourceParameters)
		{
			ChangeAudioSourceParametersMessageNetMessage msg = new ChangeAudioSourceParametersMessageNetMessage
			{
				SoundSpawnToken = soundSpawnToken,
				AudioSourceParameters = audioSourceParameters
			};

			new ChangeAudioSourceParametersMessage().SendTo(recipient, msg);
			return msg;
		}

		/// <summary>
		/// Send to all client to change the mixer of a playing sound
		/// </summary>
		/// <param name="soundSpawnToken">The token that identifies the SoundSpawn uniquely among the server and all clients </param>
		/// <param name="audioSourceParameters">The Audio Source Parameters to apply.</param>
		/// <returns>The sent message</returns>
		public static ChangeAudioSourceParametersMessageNetMessage SendToAll(string soundSpawnToken, AudioSourceParameters audioSourceParameters)
		{
			ChangeAudioSourceParametersMessageNetMessage msg = new ChangeAudioSourceParametersMessageNetMessage
			{
				SoundSpawnToken = soundSpawnToken,
				AudioSourceParameters = audioSourceParameters
			};

			new ChangeAudioSourceParametersMessage().SendToAll(msg);
			return msg;
		}
	}
}
