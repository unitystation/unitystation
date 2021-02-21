using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
///     Message that tells client which UI action to perform
/// </summary>
public class UpdateHungerStateMessage : ServerMessage
{
	public class UpdateHungerStateMessageNetMessage : NetworkMessage
	{
		public HungerState State;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as UpdateHungerStateMessageNetMessage;
		if(newMsg == null) return;

		MetabolismSystem metabolismSystem = PlayerManager.LocalPlayer.GetComponent<MetabolismSystem>();
		metabolismSystem.SetHungerState(newMsg.State);
	}

	public static UpdateHungerStateMessageNetMessage Send(GameObject recipient, HungerState state)
	{
		UpdateHungerStateMessageNetMessage msg = new UpdateHungerStateMessageNetMessage
		{
			State = state
		};
		new UpdateHungerStateMessage().SendTo(recipient, msg);
		return msg;
	}
}
