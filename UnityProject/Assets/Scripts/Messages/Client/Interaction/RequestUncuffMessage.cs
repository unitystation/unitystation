using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Sent by the client who wishes to uncuff someone when the "uncuff" button in the right click menu is pressed
/// </summary>
public class RequestUncuffMessage : ClientMessage
{
	public static short MessageType = (short)MessageTypes.RequestUncuffMessage;

	/// <summary>
	/// ID of the player who will be uncuffed
	/// </summary>
	public NetworkInstanceId PlayerToUncuff;

	public static void Send(GameObject playerToUncuff)
	{
		var msg = new RequestUncuffMessage
		{
			PlayerToUncuff = playerToUncuff.NetId()
		};
		msg.Send();
	}

	public override IEnumerator Process()
	{
		yield return WaitFor(PlayerToUncuff);
		GameObject actor = SentByPlayer.GameObject;
		GameObject playerToUncuff = NetworkObject;

		var finishProgressAction = new FinishProgressAction(
			finishReason =>
			{
				if (finishReason == FinishProgressAction.FinishReason.COMPLETED)
				{
					playerToUncuff.GetComponent<PlayerMove>().RequestUncuff(actor);
				}
			}
		);

		var restraint = playerToUncuff.GetComponent<PlayerNetworkActions>().Inventory[EquipSlot.handcuffs]?.Item?.GetComponent<Restraint>();

		if (restraint)
			UIManager.ProgressBar.StartProgress(actor.transform.position, restraint.RemoveTime, finishProgressAction, actor);
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(PlayerToUncuff);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		PlayerToUncuff = reader.ReadNetworkId();
	}
}
