using System.Collections;
using UnityEngine;
using Mirror;

namespace Messages.Server
{
	public class JobRequestFailedMessage : ServerMessage
	{
		public struct JobRequestFailedMessageNetMesasge : NetworkMessage
		{
			public JobRequestError FailReason;
		}

		//This is needed so the message can be discovered in NetworkManagerExtensions
		public JobRequestFailedMessageNetMesasge IgnoreMe;

		public override void Process<T>(T msg)
		{
			var newMsgNull = msg as JobRequestFailedMessageNetMesasge?;
			if(newMsgNull == null) return;
			var newMsg = newMsgNull.Value;

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
