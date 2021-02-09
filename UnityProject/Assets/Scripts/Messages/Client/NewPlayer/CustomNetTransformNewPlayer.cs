using System.Collections;
using Messages.Client;

public class CustomNetTransformNewPlayer: ClientMessage
{
	public uint CNT;

	public override void Process()
	{
		LoadNetworkObject(CNT);
		if (NetworkObject == null) return;
		NetworkObject.GetComponent<CustomNetTransform>()?.NotifyPlayer(
			SentByPlayer.Connection);
	}

	public static CustomNetTransformNewPlayer Send(uint netId)
	{
		CustomNetTransformNewPlayer msg = new CustomNetTransformNewPlayer
		{
			CNT = netId
		};
		msg.Send();
		return msg;
	}
}
