using System.Collections;
using UnityEngine;

/// <summary>
///     Message that tells client to play a sound at a position
/// </summary>
public class PlaySoundMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.PlaySoundMessage;
	public string SoundName;
	public Vector3 Position;
	public float Pitch;
	public bool ShakeGround;
	public byte ShakeIntensity;
	public int	ShakeRange;

	public override IEnumerator Process() {
		yield return null;
		if ( Position.RoundToInt() == TransformState.HiddenPos )
		{
			SoundManager.Play(SoundName, 1, Pitch);
		}
		else
		{
			SoundManager.PlayAtPosition(SoundName, Position, Pitch);
		}

		if ( ShakeGround )
		{
			if ( PlayerManager.LocalPlayerScript
			 && !PlayerManager.LocalPlayerScript.IsInReach( Position, ShakeRange ) )
			{
				//Don't shake if local player is out of range
				yield break;
			}
			float intensity = Mathf.Clamp(ShakeIntensity/(float)byte.MaxValue, 0.01f, 10f);
			Camera2DFollow.followControl.Shake(intensity, intensity);
		}
	}

	public static PlaySoundMessage SendToAll( string sndName, Vector3 pos, float pitch,
			bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30 ) {
		PlaySoundMessage msg = new PlaySoundMessage
		{
			SoundName = sndName,
			Position = pos,
			Pitch = pitch,
			ShakeGround = shakeGround,
			ShakeIntensity = shakeIntensity,
			ShakeRange = shakeRange
		};

		msg.SendToAll();

		return msg;
	}
	public static PlaySoundMessage Send( GameObject recipient, string sndName, Vector3 pos, float pitch,
			bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30 ) {
		PlaySoundMessage msg = new PlaySoundMessage
		{
			SoundName = sndName,
			Position = pos,
			Pitch = pitch,
			ShakeGround = shakeGround,
			ShakeIntensity = shakeIntensity,
			ShakeRange = shakeRange
		};

		msg.SendTo(recipient);

		return msg;
	}

	public override string ToString()
	{
		return $"[SoundMsg Name={SoundName}]";
	}
}
