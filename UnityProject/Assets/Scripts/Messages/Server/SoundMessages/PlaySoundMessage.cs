using AddressableReferences;
using Mirror;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Assets.Scripts.Messages.Server.SoundMessages
{
	/// <summary>
	///     Message that tells client to play a sound at a position
	/// </summary>
	public class PlaySoundMessage : ServerMessage
	{
		public string SoundAssetReferenceGuid;
		public Vector3 Position;
		///Allow this one to sound polyphonically
		public bool Polyphonic;
		public uint TargetNetId;

		// Allow to perform a camera shake effect along with the sound.
		public ShakeParameters ShakeParameters { get; set; }

		// Allow to personalize Audio Source parameters for any sound to play.
		public AudioSourceParameters AudioSourceParameters { get; set; }

		public override void Process()
		{
			if (string.IsNullOrEmpty(SoundAssetReferenceGuid))
			{
				Logger.LogError(ToString() + " has no AssetReference Guid!", Category.Audio);
				return;
			}

			bool isPositionProvided = Position.RoundToInt() != TransformState.HiddenPos;

			if (AudioSourceParameters == null)
				AudioSourceParameters = new AudioSourceParameters();

			// Recompose a list of a single AddressableAudioSoure from its primart key (Guid)
			List<AddressableAudioSource> addressableAudioSources = new List<AddressableAudioSource>() { new AddressableAudioSource(SoundAssetReferenceGuid) };

			if (isPositionProvided)
			{
				SoundManager.PlayAtPosition(addressableAudioSources, Position, Polyphonic, netId: TargetNetId, audioSourceParameters: AudioSourceParameters );
			}
			else
			{
				SoundManager.Play(addressableAudioSources, AudioSourceParameters, Polyphonic);
			}

			if (ShakeParameters != null && ShakeParameters.ShakeGround)
			{
				if (isPositionProvided
				 && PlayerManager.LocalPlayerScript
				 && !PlayerManager.LocalPlayerScript.IsInReach(Position, false, ShakeParameters.ShakeRange))
				{
					//Don't shake if local player is out of range
					return;
				}
				float intensity = Mathf.Clamp(ShakeParameters.ShakeIntensity / (float)byte.MaxValue, 0.01f, 10f);
				Camera2DFollow.followControl.Shake(intensity, intensity);
			}
		}

		public static PlaySoundMessage SendToNearbyPlayers(AddressableAudioSource addressableAudioSource, Vector3 pos,
			bool polyphonic = false,
			GameObject sourceObj = null,
			ShakeParameters shakeParameters = null,
			AudioSourceParameters audioSourceParameters = null)
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

			PlaySoundMessage msg = new PlaySoundMessage
			{
				SoundAssetReferenceGuid = addressableAudioSource.AssetReference.AssetGUID,
				Position = pos,
				Polyphonic = polyphonic,
				TargetNetId = netId,
				ShakeParameters = shakeParameters,
				AudioSourceParameters = audioSourceParameters
			};

			msg.SendToNearbyPlayers(pos);
			return msg;
		}

		public static PlaySoundMessage SendToAll(AddressableAudioSource addressableAudioSource, Vector3 pos,
			bool polyphonic = false,
			GameObject sourceObj = null,
			ShakeParameters shakeParameters = null,
			AudioSourceParameters audioSourceParameters = null)
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

			PlaySoundMessage msg = new PlaySoundMessage
			{
				SoundAssetReferenceGuid = addressableAudioSource.AssetReference.AssetGUID,
				Position = pos,
				Polyphonic = polyphonic,
				TargetNetId = netId,
				ShakeParameters = shakeParameters,
				AudioSourceParameters = audioSourceParameters
			};

			msg.SendToAll();

			return msg;
		}

		public static PlaySoundMessage Send(GameObject recipient, AddressableAudioSource addressableAudioSource, Vector3 pos,
			bool polyphonic = false,
			GameObject sourceObj = null,
			ShakeParameters shakeParameters = null,
			AudioSourceParameters audioSourceParameters = null)
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

			PlaySoundMessage msg = new PlaySoundMessage
			{
				SoundAssetReferenceGuid = addressableAudioSource.AssetReference.AssetGUID,
				Position = pos,
				Polyphonic = polyphonic,
				TargetNetId = netId,
				ShakeParameters = shakeParameters,
				AudioSourceParameters = audioSourceParameters
			};

			msg.SendTo(recipient);

			return msg;
		}

		public override string ToString()
		{
			string audioSourceParametersValue = (AudioSourceParameters == null) ? "Null" : AudioSourceParameters.ToString();
			string shakeParametersValue = (ShakeParameters == null) ? "Null" : ShakeParameters.ToString();
			return $"{nameof(SoundAssetReferenceGuid)}: {SoundAssetReferenceGuid}, {nameof(Position)}: {Position}, {nameof(Polyphonic)}: {Polyphonic}, {nameof(ShakeParameters)}: {shakeParametersValue}, {nameof(AudioSourceParameters)}: {audioSourceParametersValue}";
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.WriteString(SoundAssetReferenceGuid);
			writer.WriteVector3(Position);
			writer.WriteBoolean(Polyphonic);
			writer.WriteUInt32(TargetNetId);
			writer.WriteString(JsonConvert.SerializeObject(ShakeParameters));
			writer.WriteString(JsonConvert.SerializeObject(AudioSourceParameters));
		}

		public override void Deserialize(NetworkReader reader)
		{
			SoundAssetReferenceGuid = reader.ReadString();
			Position = reader.ReadVector3();
			Polyphonic = reader.ReadBoolean();
			TargetNetId = reader.ReadUInt32();
			ShakeParameters = JsonConvert.DeserializeObject<ShakeParameters>(reader.ReadString());
			AudioSourceParameters = JsonConvert.DeserializeObject<AudioSourceParameters>(reader.ReadString());
		}
	}
}