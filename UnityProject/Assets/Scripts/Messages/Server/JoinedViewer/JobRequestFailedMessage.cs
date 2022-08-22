using System.Collections;
using UnityEngine;
using Mirror;
using UI;

namespace Messages.Server
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
}

namespace Messages.Server
{
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
