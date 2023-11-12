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
	///     Message that tells client to play a Music at a position
	/// </summary>
	public class PlayMusicMessage : ServerMessage<PlayMusicMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string MusicAddressablePath;
			public uint TargetNetId;

			// Allow to personalize Audio Source parameters for any Music to play.
			public AudioSourceParameters AudioParameters;

			public override string ToString()
			{
				string audioSourceParametersValue = AudioParameters.ToString();
				return $"{nameof(MusicAddressablePath)}: {MusicAddressablePath}, {nameof(AudioSourceParameters)}: {audioSourceParametersValue}";
			}
		}

		public override void Process(NetMessage msg)
		{
			if (string.IsNullOrEmpty(msg.MusicAddressablePath))
			{
				Loggy.LogError(ToString() + " has no Addressable Path!", Category.Audio);
				return;
			}

			AddressableAudioSource addressableAudioSource = new AddressableAudioSource(msg.MusicAddressablePath);

		    _ = Audio.Containers.MusicManager.Instance.PlayTrack(addressableAudioSource);
		}

		/// <summary>
		/// Send a Music to be played to all clients
		/// </summary>
		/// <returns>The MusicSpawn Token generated that identifies the same Music spawn instance across server and clients</returns>
		public static void SendToAll(AddressableAudioSource addressableAudioSource,
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters())
		{
			NetMessage msg = new NetMessage
			{
				MusicAddressablePath = addressableAudioSource.AssetAddress,
				AudioParameters = audioSourceParameters,
			};

			SendToAll(msg);
		}

		/// <summary>
		/// Send a Music to be played to a specific client
		/// </summary>
		/// <returns>The MusicSpawn Token generated that identifies the same Music spawn instance across server and clients</returns>
		public static void Send(GameObject recipient, AddressableAudioSource addressableAudioSource, Vector3 pos,
			GameObject sourceObj = null,
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


			NetMessage msg = new NetMessage
			{
				MusicAddressablePath = addressableAudioSource.AssetAddress,
				TargetNetId = netId,
				AudioParameters = audioSourceParameters
			};

			SendTo(recipient, msg);
		}
	}
}
