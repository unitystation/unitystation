using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Pickupable))]
public class BackPackTrigger : InputTrigger
{
	private StorageObject storageObj;
	private ObjectBehaviour objectBehaviour;

	void Start()
	{
		var pickup = GetComponent<Pickupable>();
		if (pickup != null)
		{
			pickup.OnPickupServer.AddListener(OnPickupServer);
		}
	}

	void Awake()
	{
		storageObj = GetComponent<StorageObject>();
		objectBehaviour = GetComponent<ObjectBehaviour>();

	}
	public override bool UI_InteractOtherSlot(GameObject originator, GameObject item)
	{
		if (item != null)
		{
			//Put item in back without opening it
			//Check if it is a storage obj:
			if (storageObj.NextSpareSlot() != null)
			{
				UIManager.TryUpdateSlot(new UISlotObject(storageObj.NextSpareSlot().UUID, item,
				InventorySlotCache.GetSlotByItem(item)?.inventorySlot.UUID));
				SoundManager.PlayAtPosition("Rustle0" + UnityEngine.Random.Range(1, 6).ToString(), PlayerManager.LocalPlayer.transform.position);
				ObjectBehaviour itemObj = item.GetComponent<ObjectBehaviour>();
				itemObj.parentContainer = objectBehaviour;
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
		return false;
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		//TODO: Remove once refactored to IF2
		return false;
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
	public void OnPickupServer(HandApply interaction)
	{
		//Do a sync of the storage items when adding to UI
		storageObj.NotifyPlayer(interaction.Performer);
	}
}