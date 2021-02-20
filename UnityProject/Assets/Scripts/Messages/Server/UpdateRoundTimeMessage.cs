using System.Collections;

/// <summary>
///     Message that tells client what is the current round time
/// </summary>
public class UpdateRoundTimeMessage : ServerMessage
{
	public class UpdateRoundTimeMessageNetMessage : ActualMessage
	{
		public string Time;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as UpdateRoundTimeMessageNetMessage;
		if(newMsg == null) return;

		GameManager.Instance.SyncTime(newMsg.Time);
	}

	public static UpdateRoundTimeMessageNetMessage Send(string time)
	{
		UpdateRoundTimeMessageNetMessage msg = new UpdateRoundTimeMessageNetMessage
		{
			Time = time
		};
		new UpdateRoundTimeMessage().SendToAll(msg);
		return msg;
	}
}