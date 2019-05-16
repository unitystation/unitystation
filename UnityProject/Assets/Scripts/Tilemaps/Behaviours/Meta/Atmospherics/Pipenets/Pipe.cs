using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipe : InputTrigger
{
	public List<Pipe> nodes = new List<Pipe>();
	public bool anchored = false;


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

		if (handObj.GetComponent<WrenchTrigger>())
		{
			SoundManager.PlayAtPosition("Wrench", transform.localPosition);
			if(anchored)
			{
				Detach();
			}
			else
			{
				Attach();
			}
		}
		return true;
	}

	public virtual void Attach()
	{

	}

	public virtual void Detach()
	{

	}

}