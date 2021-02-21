using System.Collections;
using Mirror;

/// <summary>
///     Message that tells client what is the current round time
/// </summary>
public class UpdateRoundTimeMessage : ServerMessage
{
	public class UpdateRoundTimeMessageNetMessage : NetworkMessage
	{
		public string Time;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as UpdateRoundTimeMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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