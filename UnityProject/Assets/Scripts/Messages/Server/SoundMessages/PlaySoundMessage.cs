using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts.Messages.Server.SoundMessages
{
	/// <summary>
	///     Message that tells client to play a sound at a position
	/// </summary>
	public class PlaySoundMessage : ServerMessage
	{
		public string SoundName;
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
			if (string.IsNullOrEmpty(SoundName))
			{
				Logger.LogError(ToString() + " has no SoundName!", Category.Audio);
				return;
			}

			bool isPositionProvided = Position.RoundToInt() != TransformState.HiddenPos;

			if (AudioSourceParameters == null)
				AudioSourceParameters = new AudioSourceParameters();

			if (isPositionProvided)
			{
				SoundManager.PlayAtPosition(SoundName, Position, Polyphonic, netId: TargetNetId, audioSourceParameters: AudioSourceParameters );
			}
			else
			{
				SoundManager.Play(SoundName, AudioSourceParameters, Polyphonic);
			}
		
			if (ShakeParameters != null && ShakeParameters.ShakeGround)
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

		public static PlaySoundMessage SendToNearbyPlayers(string sndName, Vector3 pos,
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
				SoundName = sndName,
				Position = pos,
				Polyphonic = polyphonic,
				TargetNetId = netId,
				ShakeParameters = shakeParameters,
				AudioSourceParameters = audioSourceParameters
			};

			msg.SendToNearbyPlayers(pos);
			return msg;
		}

		public static PlaySoundMessage SendToAll(string sndName, Vector3 pos,
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
				SoundName = sndName,
				Position = pos,
				Polyphonic = polyphonic,
				TargetNetId = netId,
				ShakeParameters = shakeParameters,
				AudioSourceParameters = audioSourceParameters
			};

			msg.SendToAll();

			return msg;
		}

		public static PlaySoundMessage Send(GameObject recipient, string sndName, Vector3 pos,
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
				SoundName = sndName,
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
			return $"{nameof(SoundName)}: {SoundName}, {nameof(Position)}: {Position}, {nameof(Polyphonic)}: {Polyphonic}, {nameof(ShakeParameters)}: {shakeParametersValue}, {nameof(AudioSourceParameters)}: {audioSourceParametersValue}";
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.WriteString(SoundName);
			writer.WriteVector3(Position);
			writer.WriteBoolean(Polyphonic);
			writer.WriteUInt32(TargetNetId);
			writer.WriteString(JsonConvert.SerializeObject(ShakeParameters));
			writer.WriteString(JsonConvert.SerializeObject(AudioSourceParameters));
		}

		public override void Deserialize(NetworkReader reader)
		{
			SoundName = reader.ReadString();
			Position = reader.ReadVector3();
			Polyphonic = reader.ReadBoolean();
			TargetNetId = reader.ReadUInt32();
			ShakeParameters = JsonConvert.DeserializeObject<ShakeParameters>(reader.ReadString());
			AudioSourceParameters = JsonConvert.DeserializeObject<AudioSourceParameters>(reader.ReadString());
		}
	}
}