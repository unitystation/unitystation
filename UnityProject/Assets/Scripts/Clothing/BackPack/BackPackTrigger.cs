using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BackPackTrigger : PickUpTrigger
{
	private StorageItem storageItem;

	void Awake()
	{
		storageItem = GetComponent<StorageItem>();
	}
	public override void UI_Interact(GameObject originator, string hand)
	{
		UIManager.StorageHandler.OpenStorageUI(storageItem);
	}

	[Server]
	public override bool ValidatePickUp(GameObject originator, string handSlot = null)
	{
		//Do a sync of the storage items when adding to UI
		storageItem.NotifyPlayer(originator);

		return base.ValidatePickUp(originator, handSlot);
	}
}
