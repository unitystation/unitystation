using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BackPackTrigger : PickUpTrigger
{
	private StorageObject storageObj;

	void Awake()
	{
		storageObj = GetComponent<StorageObject>();
	}
	public override void UI_InteractOtherSlot(GameObject originator, GameObject item)
	{
		if (item != null)
		{
			//Put item in back without opening it
			//Check if it is a storage obj:
			if (storageObj.NextSpareSlot() != null)
			{
				UIManager.TryUpdateSlot(new UISlotObject(storageObj.NextSpareSlot().UUID, item,
					InventorySlotCache.GetSlotByItem(item)?.inventorySlot.UUID));
			}
		}
		else
		{
			if (UIManager.StorageHandler.storageCache != storageObj)
			{
				UIManager.StorageHandler.OpenStorageUI(storageObj);
			}
			else
			{
				UIManager.StorageHandler.CloseStorageUI();
			}
		}
	}

	public override void UI_Interact(GameObject originator, string hand)
	{
		if (UIManager.StorageHandler.storageCache != storageObj)
		{
			UIManager.StorageHandler.OpenStorageUI(storageObj);
		}
		else
		{
			UIManager.StorageHandler.CloseStorageUI();
		}
	}

	[Server]
	public override bool ValidatePickUp(GameObject originator, string handSlot = null)
	{
		//Do a sync of the storage items when adding to UI
		storageObj.NotifyPlayer(originator);

		return base.ValidatePickUp(originator, handSlot);
	}
}