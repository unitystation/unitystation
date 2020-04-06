using System.Collections;
using UnityEngine;

/// <summary>
///     Message that tells client to play an ambient track
/// </summary>
public class PlayAmbientTrack : ServerMessage
{
	public string TrackName;

	public override void Process()
	{
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