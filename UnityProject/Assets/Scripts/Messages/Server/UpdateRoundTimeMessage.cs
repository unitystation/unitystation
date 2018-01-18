using System.Collections;
using UnityEngine.Networking;

/// <summary>
///     Message that tells client what is the current round time
/// </summary>
public class UpdateRoundTimeMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.UpdateRoundTimeMessage;
	public float Time;

	public override IEnumerator Process()
	{
		GameManager.Instance.SyncTime(Time);
		yield return null;
	}

	public static UpdateRoundTimeMessage Send(float time)
	{
		UpdateRoundTimeMessage msg = new UpdateRoundTimeMessage
		{
			Time = time
		};
		msg.SendToAll();
		return msg;
	}

	public override string ToString()
	{
		return $"[UpdateRoundTimeMessage Type={MessageType} Time={Time}]";
	}
}