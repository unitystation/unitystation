using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChemistryDispenser : NetworkTabTrigger {

	public ReagentContainer Container;
	public ObjectBehaviour objectse;
	public delegate void ChangeEvent ();
	public static event ChangeEvent changeEvent;


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
				UpdateGUI();
				return true;
			}
			TabUpdateMessage.Send( originator, gameObject, NetTabType, TabAction.Open );
		} else {
			TabUpdateMessage.Send( originator, gameObject, NetTabType, TabAction.Open );
		}
		return true;
	}

	public void  UpdateGUI()
	{
		// Change event runs updateAll in ChemistryGUI
   		if(changeEvent!=null)
		{
			changeEvent();
		}
 	}


}
