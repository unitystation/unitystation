using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeTrigger : PickUpTrigger
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
				return base.Interact(originator, position, hand);
			}
		}
		else
		{
			if (handObj.GetComponent<WrenchTrigger>())
			{
				pipe.WrenchAct();
			}
		}
		return true;
	}
}
