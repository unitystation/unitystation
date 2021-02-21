using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
///     Message that tells client which UI action to perform
/// </summary>
public class UpdateHungerStateMessage : ServerMessage
{
	public struct UpdateHungerStateMessageNetMessage : NetworkMessage
	{
		public HungerState State;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public UpdateHungerStateMessageNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as UpdateHungerStateMessageNetMessage?;
		if(newMsgNull == null) return;
		var newMsg = newMsgNull.Value;

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
