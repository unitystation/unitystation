using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class PipeTrigger : InputTrigger
{
	Pipe pipe;

	public void Awake()
	{
		pipe = GetComponent<Pipe>();
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!CanUse(originator, hand, position, false))
		{
			return false;
		}
		if (!isServer)
		{
			//ask server to perform the interaction
			InteractMessage.Send(gameObject, position, hand);
			return true;
		}

		PlayerNetworkActions pna = originator.GetComponent<PlayerNetworkActions>();
		GameObject handObj = pna.Inventory[hand].Item;

		if (handObj == null)
		{
			if (!pipe.objectBehaviour.isNotPushable)
			{
				return false;
			}
		}
		else
		{
			var tool = handObj.GetComponent<Tool>();
			if (tool != null && tool.ToolType == ToolType.Wrench)
			{
				pipe.WrenchAct();
			}
		}
		return true;
	}
}
