using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoConsole : NetworkTabTrigger
{
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!CanUse(originator, hand, position, false))
		{
			return false;
		}
		if (!isServer)
		{
			InteractMessage.Send(gameObject, position, hand);
		}
		else
		{
			TabUpdateMessage.Send(originator, gameObject, NetTabType, TabAction.Open);
		}

		return true;
	}

	public void UpdateGUI()
	{
		//Update GUI_Cargo
	}
}
