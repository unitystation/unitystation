using System.Collections;
using UnityEngine;
using Mirror;

namespace Messages.Server
{
	public class JobRequestFailedMessage : ServerMessage
	{
		public JobRequestError FailReason;

		public override void Process()
		{
			UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().ShowFailMessage(FailReason);
		}

		public static JobRequestFailedMessage SendTo(ConnectedPlayer recipient, JobRequestError failReason)
		{
			var msg = new JobRequestFailedMessage
			{
				FailReason = failReason,
			};

			msg.SendTo(recipient);
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
