using Mirror;

/// <summary>
///Message that tells client the status of the preround countdown
/// </summary>
public class UpdateCountdownMessage : ServerMessage
{
	public class UpdateCountdownMessageNetMessage : ActualMessage
	{
		public bool Started;
		public double EndTime;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as UpdateCountdownMessageNetMessage;
		if(newMsg == null) return;

		UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().SyncCountdown(newMsg.Started, newMsg.EndTime);
	}

	/// <summary>
	/// Calculates when the countdown will end from the time left and sends it to all clients
	/// </summary>
	/// <param name="started">Has the countdown started or stopped?</param>
	/// <param name="time">How much time is left on the countdown?</param>
	/// <returns></returns>
	public static UpdateCountdownMessageNetMessage Send(bool started, float time)
	{
		// Calculate when the countdown will end relative to the current NetworkTime
		double endTime = NetworkTime.time + time;
		UpdateCountdownMessageNetMessage msg = new UpdateCountdownMessageNetMessage
		{
			Started = started,
			EndTime = endTime
		};
		new UpdateCountdownMessage().SendToAll(msg);
		return msg;
	}
}