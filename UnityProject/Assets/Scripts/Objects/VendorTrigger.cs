using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VendorTrigger : NetworkTabTrigger
{
	public List<VendorItem> VendorContent = new List<VendorItem>();
	public Color HullColor = Color.white;
	public bool EjectObjects = false;
	public EjectDirection EjectDirection = EjectDirection.None;
	public VendorUpdateEvent OnRestockUsed = new VendorUpdateEvent();

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!CanUse(originator, hand, position, false))
		{
			return false;
		}
		if (!isServer)
		{
			InteractMessage.Send(gameObject, position, hand);
			return true;
		}

		TabUpdateMessage.Send(originator, gameObject, NetTabType, TabAction.Open);

		//Checking restock
		PlayerScript ps = originator.GetComponent<PlayerScript>();
		if (!ps || ps.canNotInteract() || !ps.IsInReach(position, true))
		{
			return true;
		}

		var slot = InventoryManager.GetSlotFromOriginatorHand(originator, hand);
		var restock = slot.Item?.GetComponentInChildren<VendingRestock>();
		if (restock != null)
		{
			OnRestockUsed?.Invoke();
			GameObject item = ps.playerNetworkActions.Inventory[hand].Item;
			InventoryManager.UpdateInvSlot(true, "", slot.Item, slot.UUID);
		}

		return true;
	}
}

public enum EjectDirection { None, Up, Down, Random }

public class VendorUpdateEvent: UnityEvent {}

//Adding this as a separate class so we can easily extend it in future -
//add price or required access, stock amount and etc.
[System.Serializable]
public class VendorItem
{
	public GameObject Item;
	public int Stock = 5;

	public VendorItem(VendorItem item)
	{
		this.Item = item.Item;
		this.Stock = item.Stock;
	}
}