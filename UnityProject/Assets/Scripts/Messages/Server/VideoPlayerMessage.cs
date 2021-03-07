using System.Collections;
using Mirror;

namespace Messages.Server
{
	/// <summary>
	///     Message that tells client to player a certain video in the video player
	/// </summary>
	public class VideoPlayerMessage : ServerMessage<VideoPlayerMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public VideoType VideoToPlay;
		}

		public override void Process(NetMessage msg)
		{
			switch (msg.VideoToPlay)
			{
				case VideoType.NukeVid:
					UIManager.Display.VideoPlayer.PlayNukeDetVideo();
					break;
				case VideoType.RestartRound:
					UIManager.Display.VideoPlayer.PlayRoundRestartVideo();
					break;
			}
		}

		public static NetMessage Send(VideoType videoType)
		{
			NetMessage msg = new NetMessage
			{
				VideoToPlay = videoType
			};

			SendToAll(msg);
			return msg;
		}
	}

	public enum VideoType
	{
		NukeVid,
		RestartRound
	}
}