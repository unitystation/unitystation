using System.Collections;

/// <summary>
///     Message that tells client what is the current round time
/// </summary>
public class UpdateRoundTimeMessage : ServerMessage
{
	public string Time;

	public override void Process()
	{
		GameManager.Instance.SyncTime(Time);
	}

	public static UpdateRoundTimeMessage Send(string time)
	{
		UpdateRoundTimeMessage msg = new UpdateRoundTimeMessage
		{
			Time = time
		};
		msg.SendToAll();
		return msg;
	}
}