using System;
using System.Collections;

/// <summary>
///     Message that tells client the status of the preround countdown
/// </summary>
public class UpdateCountdownMessage : ServerMessage
{
	public bool Started;
	public long EndTime;

	public override void Process()
	{
		UIManager.Display.preRoundWindow.GetComponent<GUI_PreRoundWindow>().SyncCountdown(Started, EndTime);
	}

	/// <summary>
	/// Calculates when the countdown will end from the time left and sends it to all clients
	/// </summary>
	/// <param name="started">Has the countdown started or stopped?</param>
	/// <param name="time">How much time is left on the countdown?</param>
	/// <returns></returns>
	public static UpdateCountdownMessage Send(bool started, float time)
	{
		// Calculate when the countdown will end in the unix timestamp
		long endTime = DateTimeOffset.UtcNow.AddSeconds(time).ToUnixTimeMilliseconds();
		UpdateCountdownMessage msg = new UpdateCountdownMessage
		{
			Started = started,
			EndTime = endTime
		};
		msg.SendToAll();
		return msg;
	}
}