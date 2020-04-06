using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Sent by the client who wishes to uncuff someone when the "uncuff" button in the right click menu is pressed
/// </summary>
public class RequestUncuffMessage : ClientMessage
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Uncuff);

	//TODO: This class shouldn't be needed, IF2 can be used instead

	/// <summary>
	/// ID of the player who will be uncuffed
	/// </summary>
	public uint PlayerToUncuff;

	public static void Send(GameObject playerToUncuff)
	{
		var msg = new RequestUncuffMessage
		{
			PlayerToUncuff = playerToUncuff.NetId()
		};
		msg.Send();
	}

	public override void Process()
	{
		LoadNetworkObject(PlayerToUncuff);
		GameObject actor = SentByPlayer.GameObject;
		GameObject playerToUncuff = NetworkObject;

		var handcuffs = playerToUncuff.GetComponent<ItemStorage>().GetNamedItemSlot(NamedSlot.handcuffs).ItemObject;

		if (handcuffs != null)
		{
			var restraint = handcuffs.GetComponent<Restraint>();
			if (restraint)
			{
				void ProgressComplete()
				{
					playerToUncuff.GetComponent<PlayerMove>().RequestUncuff(actor);
				}

				StandardProgressAction.Create(ProgressConfig, ProgressComplete)
					.ServerStartProgress(playerToUncuff.RegisterTile(), restraint.RemoveTime, actor);
			}
		}
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(PlayerToUncuff);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		PlayerToUncuff = reader.ReadUInt32();
	}
}
