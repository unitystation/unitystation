using Mirror;
using UI;
using UI.Systems.PreRound;

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
			public RoundState RoundState;
		}

		public override void Process(NetMessage msg)
		{
			GameManager.Instance.CurrentRoundState = msg.RoundState;
			UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().CountdownArea.SyncCountdown(msg.Started, msg.EndTime);
		}

		/// <summary>
		/// Calculates when the countdown will end from the time left and sends it to all clients
		/// </summary>
		/// <param name="started">Has the countdown started or stopped?</param>
		/// <param name="time">How much time is left on the countdown?</param>
		/// <returns></returns>
		public static NetMessage Send(bool started, float time, RoundState state)
		{
			// Calculate when the countdown will end relative to the current NetworkTime
			double endTime = NetworkTime.time + time;
			NetMessage msg = new NetMessage
			{
				Started = started,
				EndTime = endTime,
				RoundState = state
			};

			SendToAll(msg);
			return msg;
		}
	}
}
