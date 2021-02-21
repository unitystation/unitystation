using Mirror;

/// <summary>
///Message that tells client the status of the preround countdown
/// </summary>
public class UpdateCountdownMessage : ServerMessage
{
	public struct UpdateCountdownMessageNetMessage : NetworkMessage
	{
		public bool Started;
		public double EndTime;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public UpdateCountdownMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as UpdateCountdownMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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