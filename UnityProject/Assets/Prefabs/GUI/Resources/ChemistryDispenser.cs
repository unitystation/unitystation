using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChemistryDispenser : NetworkTabTrigger {

	public ReagentContainer Container;
	public ObjectBehaviour objectse;
	public GameObject ofthis; //this 


	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		ofthis = this.gameObject;
		var playerScript = originator.GetComponent<PlayerScript>();
		if (playerScript.canNotInteract() || !playerScript.IsInReach( gameObject ))
		{ //check for both client and server
			return true;
		}

		if (!isServer)
		{ 
			//Client wants this code to be run on server
			InteractMessage.Send(gameObject, hand);
		}
		else
		{
			//Server actions
			if (Container == null){
				PlayerScript ps = originator.GetComponent<PlayerScript>();
				if (ps.canNotInteract() || !ps.IsInReach(position))
				{
					return false;
				}
				var slot = InventoryManager.GetSlotFromOriginatorHand(originator, hand);
				var stContainer = slot.Item?.GetComponentInChildren<ReagentContainer>();
				if (stContainer != null)
				{
					Container = stContainer;
					//Logger.Log ("set!!");
					GameObject item = ps.playerNetworkActions.Inventory[hand].Item;
					objectse = item.GetComponentInChildren<ObjectBehaviour> ();
					InventoryManager.UpdateInvSlot(true, "", slot.Item, slot.UUID);
					return true;
				}
				TabUpdateMessage.Send( originator, gameObject, NetTabType, TabAction.Open );
			} else {
				TabUpdateMessage.Send( originator, gameObject, NetTabType, TabAction.Open );
			}


		}
		return true;
	}



}
