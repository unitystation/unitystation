﻿using System.Collections;
using Doors;
using Mirror;

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
			else if (NetworkObject.TryGetComponent(out DoorControllerV2 doorControllerV2))
			{
				doorControllerV2.UpdateNewPlayer(SentByPlayer.Connection);
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

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Door = reader.ReadUInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(Door);
	}
}