using AddressableReferences;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoundMessages
{
	/// <summary>
	///     Message that tells client to play a sound at a position
	/// </summary>
	public class PlaySoundMessage : ServerMessage
	{
		public string SoundAddressablePath;
		public Vector3 Position;
		///Allow this one to sound polyphonically
		public bool Polyphonic;
		public uint TargetNetId;
		public string SoundSpawnToken;

		// Allow to perform a camera shake effect along with the sound.
		public ShakeParameters ShakeParameters;

		// Allow to personalize Audio Source parameters for any sound to play.
		public AudioSourceParameters AudioParameters;

		public override void Process()
		{
			if (string.IsNullOrEmpty(SoundAddressablePath))
			{
				Logger.LogError(ToString() + " has no Addressable Path!", Category.Audio);
				return;
			}

			bool isPositionProvided = Position.RoundToInt() != TransformState.HiddenPos;

			// Recompose a list of a single AddressableAudioSource from its primary key (Guid)
			List<AddressableAudioSource> addressableAudioSources = new List<AddressableAudioSource>() { new AddressableAudioSource(SoundAddressablePath) };

			if (isPositionProvided)
			{
				SoundManager.PlayAtPosition(addressableAudioSources, Position, SoundSpawnToken, Polyphonic, netId: TargetNetId, audioSourceParameters: AudioParameters);
			}
			else
			{
				SoundManager.Play(addressableAudioSources, SoundSpawnToken, AudioParameters, Polyphonic);
			}

			if (ShakeParameters.ShakeGround)
			{
				if (isPositionProvided
				 && PlayerManager.LocalPlayerScript
				 && !PlayerManager.LocalPlayerScript.IsPositionReachable(Position, false, ShakeParameters.ShakeRange))
				{
					//Don't shake if local player is out of range
					return;
				}
				float intensity = Mathf.Clamp(ShakeParameters.ShakeIntensity / (float)byte.MaxValue, 0.01f, 10f);
				Camera2DFollow.followControl.Shake(intensity, intensity);
			}
		}

		/// <summary>
		/// Send a sound to be played to all nearby players
		/// </summary>
		/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
		public static string SendToNearbyPlayers(AddressableAudioSource addressableAudioSource, Vector3 pos,
			bool polyphonic = false, GameObject sourceObj = null, 
			ShakeParameters shakeParameters = new ShakeParameters(),
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters())
		{
			var netId = NetId.Empty;
			if (sourceObj != null)
			{
				var netB = sourceObj.GetComponent<NetworkBehaviour>();
				if (netB != null)
				{
					netId = netB.netId;
				}
			}

			string soundSpawnToken = Guid.NewGuid().ToString();

			PlaySoundMessage msg = new PlaySoundMessage
			{
				SoundAddressablePath = addressableAudioSource.AssetAddress,
				Position = pos,
				Polyphonic = polyphonic,
				TargetNetId = netId,
				ShakeParameters = shakeParameters,
				AudioParameters = audioSourceParameters,
				SoundSpawnToken = soundSpawnToken
			};

			msg.SendToNearbyPlayers(pos);
			return soundSpawnToken;
		}

		/// <summary>
		/// Send a sound to be played to all clients
		/// </summary>
		/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
		public static string SendToAll(AddressableAudioSource addressableAudioSource, Vector3 pos,
			bool polyphonic = false, GameObject sourceObj = null, 
			ShakeParameters shakeParameters = new ShakeParameters(),
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters())
		{
			var netId = NetId.Empty;
			if (sourceObj != null)
			{
				var netB = sourceObj.GetComponent<NetworkBehaviour>();
				if (netB != null)
				{
					netId = netB.netId;
				}
			}

			string soundSpawnToken = Guid.NewGuid().ToString();

			PlaySoundMessage msg = new PlaySoundMessage
			{
				SoundAddressablePath = addressableAudioSource.AssetAddress,
				Position = pos,
				Polyphonic = polyphonic,
				TargetNetId = netId,
				ShakeParameters = shakeParameters,
				AudioParameters = audioSourceParameters,
				SoundSpawnToken = soundSpawnToken
			};
			
			msg.SendToAll();

			return soundSpawnToken;
		}

		/// <summary>
		/// Send a sound to be played to a specific client
		/// </summary>
		/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
		public static string Send(GameObject recipient, AddressableAudioSource addressableAudioSource, Vector3 pos,
			bool polyphonic = false, GameObject sourceObj = null, 
			ShakeParameters shakeParameters = new ShakeParameters(),
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters())
		{
			var netId = NetId.Empty;
			if (sourceObj != null)
			{
				var netB = sourceObj.GetComponent<NetworkBehaviour>();
				if (netB != null)
				{
					netId = netB.netId;
				}
			}

			string soundSpawnToken = Guid.NewGuid().ToString();

			PlaySoundMessage msg = new PlaySoundMessage
			{
				SoundAddressablePath = addressableAudioSource.AssetAddress,
				Position = pos,
				Polyphonic = polyphonic,
				TargetNetId = netId,
				ShakeParameters = shakeParameters,
				AudioParameters = audioSourceParameters,
				SoundSpawnToken = soundSpawnToken
			};

			msg.SendTo(recipient);

			return soundSpawnToken;
		}

		public override string ToString()
		{
			string audioSourceParametersValue = AudioParameters.ToString();
			string shakeParametersValue = ShakeParameters.ToString();
			return $"{nameof(SoundAddressablePath)}: {SoundAddressablePath}, {nameof(Position)}: {Position}, {nameof(Polyphonic)}: {Polyphonic}, {nameof(ShakeParameters)}: {shakeParametersValue}, {nameof(AudioSourceParameters)}: {audioSourceParametersValue}";
		}
	}
}
