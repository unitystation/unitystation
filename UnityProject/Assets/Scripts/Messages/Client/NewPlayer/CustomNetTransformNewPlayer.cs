using System.Collections;
using Messages.Client;

public class CustomNetTransformNewPlayer : ClientMessage
{
	public class CustomNetTransformNewPlayerNetMessage : ActualMessage
	{
		public uint CNT;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as CustomNetTransformNewPlayerNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.CNT);
		if (NetworkObject == null) return;
		NetworkObject.GetComponent<CustomNetTransform>()?.NotifyPlayer(
			SentByPlayer.Connection);
	}

	public static CustomNetTransformNewPlayerNetMessage Send(uint netId)
	{
		CustomNetTransformNewPlayerNetMessage msg = new CustomNetTransformNewPlayerNetMessage
		{
			CNT = netId
		};
		new CustomNetTransformNewPlayer().Send(msg);
		return msg;
	}
}
