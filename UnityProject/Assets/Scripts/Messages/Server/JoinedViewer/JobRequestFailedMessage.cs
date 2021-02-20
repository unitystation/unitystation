using System.Collections;
using UnityEngine;
using Mirror;

namespace Messages.Server
{
	public class JobRequestFailedMessage : ServerMessage
	{
		public class JobRequestFailedMessageNetMesasge : ActualMessage
		{
			public JobRequestError FailReason;
		}

		public override void Process(ActualMessage msg)
		{
			var newMsg = msg as JobRequestFailedMessageNetMesasge;
			if(newMsg == null) return;

			UIManager.Display.jobSelectWindow.GetComponent<GUI_PlayerJobs>().ShowFailMessage(newMsg.FailReason);
		}

		public static JobRequestFailedMessageNetMesasge SendTo(ConnectedPlayer recipient, JobRequestError failReason)
		{
			var msg = new JobRequestFailedMessageNetMesasge
			{
				FailReason = failReason,
			};

			new JobRequestFailedMessage().SendTo(recipient, msg);
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
