using Mirror;
using UI;

namespace Messages.Server
{
	/// <summary>
	///Message that tells client the status of the preround countdown
	/// </summary>
	public class UpdateCountdownMessage : ServerMessage<UpdateCountdownMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool Started;
			public double EndTime;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().SyncCountdown(msg.Started, msg.EndTime);
		}

		/// <summary>
		/// Calculates when the countdown will end from the time left and sends it to all clients
		/// </summary>
		/// <param name="started">Has the countdown started or stopped?</param>
		/// <param name="time">How much time is left on the countdown?</param>
		/// <returns></returns>
		public static NetMessage Send(bool started, float time)
		{
			// Calculate when the countdown will end relative to the current NetworkTime
			double endTime = NetworkTime.time + time;
			NetMessage msg = new NetMessage
			{
				Started = started,
				EndTime = endTime
			};

			SendToAll(msg);
			return msg;
		}
	}
}
