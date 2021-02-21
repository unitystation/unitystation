using System.Collections;
using Messages.Client;
using Mirror;
using Doors;

public class DoorNewPlayer : ClientMessage
{
	public class DoorNewPlayerNetMessage : NetworkMessage
	{
		public uint Door;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as DoorNewPlayerNetMessage;
		if(newMsg == null) return;

		// LoadNetworkObject returns bool, so it can be used to check if object is loaded correctly
		if (LoadNetworkObject(newMsg.Door))
		{
			// https://docs.unity3d.com/2019.3/Documentation/ScriptReference/Component.TryGetComponent.html
			if (NetworkObject.TryGetComponent(out DoorController doorController))
			{
				doorController.UpdateNewPlayer(SentByPlayer.Connection);
			}
		}
	}

	public static DoorNewPlayerNetMessage Send(uint netId)
	{
		DoorNewPlayerNetMessage msg = new DoorNewPlayerNetMessage
		{
			Door = netId
		};
		new DoorNewPlayer().Send(msg);
		return msg;
	}
}
