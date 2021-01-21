using System.Collections;
using Messages.Client;
using Mirror;
using Doors;

public class DoorNewPlayer: ClientMessage
{
	public uint Door;

	public override void Process()
	{
		// LoadNetworkObject returns bool, so it can be used to check if object is loaded correctly
		if (LoadNetworkObject(Door))
		{
			// https://docs.unity3d.com/2019.3/Documentation/ScriptReference/Component.TryGetComponent.html
			if (NetworkObject.TryGetComponent(out DoorController doorController))
			{
				doorController.UpdateNewPlayer(SentByPlayer.Connection);
			}
		}
	}

	public static DoorNewPlayer Send(uint netId)
	{
		DoorNewPlayer msg = new DoorNewPlayer
		{
			Door = netId
		};
		msg.Send();
		return msg;
	}
}
