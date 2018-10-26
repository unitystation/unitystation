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
	public override void UI_Interact(GameObject originator, string hand)
	{
		UIManager.StorageHandler.OpenStorageUI(storageObj);
	}

	[Server]
	public override bool ValidatePickUp(GameObject originator, string handSlot = null)
	{
		//Do a sync of the storage items when adding to UI
		storageObj.NotifyPlayer(originator);

		return base.ValidatePickUp(originator, handSlot);
	}
}
