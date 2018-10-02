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

	public override IEnumerator Process() {
		yield return null;

		SoundManager.PlayAtPosition(SoundName, Position, Pitch);
	}

	public static PlaySoundMessage SendToAll( string sndName, Vector3 pos, float pitch ) {
		PlaySoundMessage msg = new PlaySoundMessage{ SoundName = sndName,
		 Position = pos,
		 Pitch = pitch};

		msg.SendToAll();

		return msg;
	}

	public override string ToString()
	{
		return $"[SoundMsg Name={SoundName}]";
	}
}
