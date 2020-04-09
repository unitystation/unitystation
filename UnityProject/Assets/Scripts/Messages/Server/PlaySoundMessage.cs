﻿using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
///     Message that tells client to play a sound at a position
/// </summary>
public class PlaySoundMessage : ServerMessage
{
	public string SoundName;
	public Vector3 Position;
	public float Pitch;
	public bool ShakeGround;
	public byte ShakeIntensity;
	public int	ShakeRange;
	///Allow this one to sound polyphonically
	public bool Polyphonic;

	public uint TargetNetId;

	public override void Process() {

		if (string.IsNullOrEmpty(SoundName))
		{
			Logger.LogError(ToString()+" has no SoundName!", Category.Audio);
			return;
		}

		bool isPositionProvided = Position.RoundToInt() != TransformState.HiddenPos;

		if ( isPositionProvided )
		{
			SoundManager.PlayAtPosition( SoundName, Position, Pitch, Polyphonic, netId: TargetNetId );
		} else
		{
			SoundManager.Play( SoundName, 1, Pitch, 0f, Polyphonic );
		}

		if ( ShakeGround )
		{
			if ( isPositionProvided
			 && PlayerManager.LocalPlayerScript
			 && !PlayerManager.LocalPlayerScript.IsInReach( Position, false, ShakeRange ) )
			{
				//Don't shake if local player is out of range
				return;
			}
			float intensity = Mathf.Clamp(ShakeIntensity/(float)byte.MaxValue, 0.01f, 10f);
			Camera2DFollow.followControl.Shake(intensity, intensity);
		}
	}

	public static PlaySoundMessage SendToNearbyPlayers(string sndName, Vector3 pos, float pitch,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30, GameObject sourceObj = null)
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
			Pitch = pitch,
			ShakeGround = shakeGround,
			ShakeIntensity = shakeIntensity,
			ShakeRange = shakeRange,
			Polyphonic = polyphonic,
			TargetNetId = netId
		};

		msg.SendToNearbyPlayers(pos);
		return msg;
	}

	public static PlaySoundMessage SendToAll( string sndName, Vector3 pos, float pitch,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30, GameObject sourceObj = null ) {

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
			Pitch = pitch,
			ShakeGround = shakeGround,
			ShakeIntensity = shakeIntensity,
			ShakeRange = shakeRange,
			Polyphonic = polyphonic,
			TargetNetId = netId
		};

		msg.SendToAll();

		return msg;
	}
	public static PlaySoundMessage Send( GameObject recipient, string sndName, Vector3 pos, float pitch,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30, GameObject sourceObj = null ) {

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
			Pitch = pitch,
			ShakeGround = shakeGround,
			ShakeIntensity = shakeIntensity,
			ShakeRange = shakeRange,
			Polyphonic = polyphonic,
			TargetNetId = netId
		};

		msg.SendTo(recipient);

		return msg;
	}

	public override string ToString()
	{
		return $"{nameof(SoundName)}: {SoundName}, {nameof(Position)}: {Position}, {nameof(Pitch)}: {Pitch}, {nameof(ShakeGround)}: {ShakeGround}, {nameof(ShakeIntensity)}: {ShakeIntensity}, {nameof(ShakeRange)}: {ShakeRange}, {nameof(Polyphonic)}: {Polyphonic}";
	}
}
