using System;
using System.Collections.Generic;
using AddressableReferences;
using Logs;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

namespace Messages.Server.SoundMessages
{
	/// <summary>
	///     Message that tells client to play a sound at a position
	/// </summary>
	public class PlaySoundMessage : ServerMessage<PlaySoundMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string SoundAddressablePath;
			public Vector3 Position;
			///Allow this one to sound polyphonically
			public bool Polyphonic;
			public uint TargetNetId;
			public string SoundSpawnToken;
			public GameObject SourceObj;
			public bool AttachedToSource;

			// Allow to perform a camera shake effect along with the sound.
			public ShakeParameters ShakeParameters;

			// Allow to personalize Audio Source parameters for any sound to play.
			public AudioSourceParameters AudioParameters;

			public override string ToString()
			{
				string audioSourceParametersValue = AudioParameters.ToString();
				string shakeParametersValue = ShakeParameters.ToString();
				return $"{nameof(SoundAddressablePath)}: {SoundAddressablePath}, {nameof(Position)}: {Position}, {nameof(Polyphonic)}: {Polyphonic}, {nameof(ShakeParameters)}: {shakeParametersValue}, {nameof(AudioSourceParameters)}: {audioSourceParametersValue}";
			}
		}

		public override void Process(NetMessage msg)
		{
			if (string.IsNullOrEmpty(msg.SoundAddressablePath))
			{
				Loggy.LogError(ToString() + " has no Addressable Path!", Category.Audio);
				return;
			}

			bool isPositionProvided = msg.Position.RoundToInt() != TransformState.HiddenPos;

			// Recompose a list of a single AddressableAudioSource from its primary key (Guid)
			List<AddressableAudioSource> addressableAudioSources = new List<AddressableAudioSource>() { new AddressableAudioSource(msg.SoundAddressablePath) };

			if (msg.ShakeParameters.ShakeGround)
			{
				ShakeBehavior(isPositionProvided, msg);
			}

			if (msg.AttachedToSource && msg.SourceObj != null)
			{
				SoundManager.PlayAtPositionAttached(addressableAudioSources, msg.Position, msg.SourceObj,
					Guid.NewGuid().ToString(), msg.Polyphonic, true, msg.AudioParameters);
				return;
			}
			_ = isPositionProvided ?
				SoundManager.PlayAtPosition(addressableAudioSources, msg.Position, msg.SoundSpawnToken, msg.Polyphonic, netId: msg.TargetNetId, audioSourceParameters: msg.AudioParameters)
				: SoundManager.Play(addressableAudioSources, msg.SoundSpawnToken, msg.AudioParameters, msg.Polyphonic);
		}

		private void ShakeBehavior(bool isPositionProvided, NetMessage msg)
		{
			if (isPositionProvided
			    && PlayerManager.LocalPlayerScript
			    && !PlayerManager.LocalPlayerScript.IsPositionReachable(msg.Position, false, msg.ShakeParameters.ShakeRange))
			{
				//Don't shake if local player is out of range
				return;
			}
			float intensity = Mathf.Clamp(msg.ShakeParameters.ShakeIntensity / (float)byte.MaxValue, 0.01f, 10f);
			Camera2DFollow.followControl.Shake(intensity, intensity);
		}

		/// <summary>
		/// Send a sound to be played to all nearby players
		/// </summary>
		/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
		public static string SendToNearbyPlayers(AddressableAudioSource addressableAudioSource, Vector3 pos,
			bool polyphonic = false, GameObject sourceObj = null,
			ShakeParameters shakeParameters = new ShakeParameters(),
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool attachedToSource = false)
		{
			var netId = NetId.Empty;
			if (sourceObj != null)
			{
				var netB = sourceObj.GetRootGameObject().GetComponent<NetworkBehaviour>();
				if (netB != null)
				{
					netId = netB.netId;
				}
			}

			string soundSpawnToken = Guid.NewGuid().ToString();

			NetMessage msg = new NetMessage
			{
				SoundAddressablePath = addressableAudioSource.AssetAddress,
				Position = pos,
				Polyphonic = polyphonic,
				TargetNetId = netId,
				ShakeParameters = shakeParameters,
				AudioParameters = audioSourceParameters,
				SoundSpawnToken = soundSpawnToken,
				AttachedToSource = attachedToSource,
				SourceObj = sourceObj
			};

			SendToNearbyPlayers(pos, msg);
			return soundSpawnToken;
		}

		/// <summary>
		/// Send a sound to be played to all clients
		/// </summary>
		/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
		public static string SendToAll(AddressableAudioSource addressableAudioSource, Vector3 pos,
			bool polyphonic = false, GameObject sourceObj = null,
			ShakeParameters shakeParameters = new ShakeParameters(),
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool attachedToSource = false)
		{
			var netId = NetId.Empty;
			if (sourceObj != null)
			{
				var netB = sourceObj.GetRootGameObject().GetComponent<NetworkBehaviour>();
				if (netB != null)
				{
					netId = netB.netId;
				}
			}

			string soundSpawnToken = Guid.NewGuid().ToString();

			NetMessage msg = new NetMessage
			{
				SoundAddressablePath = addressableAudioSource.AssetAddress,
				Position = pos,
				Polyphonic = polyphonic,
				TargetNetId = netId,
				ShakeParameters = shakeParameters,
				AudioParameters = audioSourceParameters,
				SoundSpawnToken = soundSpawnToken,
				SourceObj = sourceObj,
				AttachedToSource = attachedToSource
			};

			SendToAll(msg);
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

			NetMessage msg = new NetMessage
			{
				SoundAddressablePath = addressableAudioSource.AssetAddress,
				Position = pos,
				Polyphonic = polyphonic,
				TargetNetId = netId,
				ShakeParameters = shakeParameters,
				AudioParameters = audioSourceParameters,
				SoundSpawnToken = soundSpawnToken
			};

			SendTo(recipient, msg);
			return soundSpawnToken;
		}
	}
}
