using System.Collections;
using Mirror;

/// <summary>
///     Message that tells client to player a certain video in the video player
/// </summary>
public class VideoPlayerMessage : ServerMessage
{
	public class VideoPlayerMessageNetMessage : NetworkMessage
	{
		public VideoType VideoToPlay;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as VideoPlayerMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		switch (newMsg.VideoToPlay)
		{
			case VideoType.NukeVid:
				UIManager.Display.VideoPlayer.PlayNukeDetVideo();
				break;
			case VideoType.RestartRound:
				UIManager.Display.VideoPlayer.PlayRoundRestartVideo();
				break;
		}
	}

	public static VideoPlayerMessageNetMessage Send(VideoType videoType)
	{
		VideoPlayerMessageNetMessage msg = new VideoPlayerMessageNetMessage
		{
			VideoToPlay = videoType
		};
		new VideoPlayerMessage().SendToAll(msg);
		return msg;
	}
}

public enum VideoType
{
	NukeVid,
	RestartRound
}