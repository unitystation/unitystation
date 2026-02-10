using Mirror;
using US13.Managers;
using US13.UI.Systems;
using US13.UI.Systems.Jobs;

namespace US13.Messages.Server.JoinedViewer
{
	public class JobRequestFailedMessage : ServerMessage<JobRequestFailedMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public JobRequestError FailReason;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().ShowFailMessage(msg.FailReason);
		}

		public static NetMessage SendTo(PlayerInfo recipient, JobRequestError failReason)
		{
			var msg = new NetMessage
			{
				FailReason = failReason,
			};

			SendTo(recipient, msg);
			return msg;
		}
	}

	public enum JobRequestError
	{
		None = 0,
		InvalidUserID = 1,
		InvalidPlayerID = 2,
		RoundNotReady = 3,
		JobBanned = 4,
		PositionsFilled = 5,
		InvalidScript = 6,
	}
}