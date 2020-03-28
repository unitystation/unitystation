using System.Collections;
using UnityEngine;

/// <summary>
///     Message that tells client to play an ambient track
/// </summary>
public class PlayAmbientTrack : ServerMessage
{
	public override short MessageType => (short) MessageTypes.PlayAmbientTrack;
	public string TrackName;

	public override IEnumerator Process()
	{
		yield return null;
		SoundManager.PlayAmbience(TrackName);
	}

	public static PlayAmbientTrack Send(GameObject recipient, string trackName)
	{
		PlayAmbientTrack msg = new PlayAmbientTrack
		{
			TrackName = trackName,
		};

		msg.SendTo(recipient);
		return msg;
	}
}