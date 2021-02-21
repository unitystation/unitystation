using System.Collections;
using Messages.Client;
using Mirror;

public class CustomNetTransformNewPlayer : ClientMessage
{
	public struct CustomNetTransformNewPlayerNetMessage : NetworkMessage
	{
		public uint CNT;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public CustomNetTransformNewPlayerNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as CustomNetTransformNewPlayerNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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
