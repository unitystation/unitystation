using System.Collections;

/// <summary>
///     Message that tells client to player a certain video in the video player
/// </summary>
public class VideoPlayerMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.VideoPlayerMessage;
	public VideoType VideoToPlay;

	public override IEnumerator Process()
	{
		switch (VideoToPlay)
		{
			case VideoType.NukeVid:
				UIManager.Display.VideoPlayer.PlayNukeDetVideo();
				break;
			case VideoType.RestartRound:
				UIManager.Display.VideoPlayer.PlayRoundRestartVideo();
				break;
		}
		yield return null;
	}

	public static VideoPlayerMessage Send(VideoType videoType)
	{
		VideoPlayerMessage msg = new VideoPlayerMessage
		{
			VideoToPlay = videoType
		};
		msg.SendToAll();
		return msg;
	}
}

public enum VideoType
{
	NukeVid,
	RestartRound
}