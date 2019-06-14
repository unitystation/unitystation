using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Allows a storage object to be interacted with in inventory, to open/close it and drag things
/// into it.
/// </summary>
[RequireComponent(typeof(StorageObject))]
[RequireComponent(typeof(Pickupable))]
public class InteractableStorage : MonoBehaviour, IInteractable<HandActivate>, IInteractable<InventoryApply>
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

	public bool Interact(InventoryApply interaction)
	{
		if (interaction.TargetObject != gameObject)
		{
			//backpack can't be "applied" to something else in inventory
			return false;
		}
		if (interaction.HandObject != null)
		{
			//Put item in back without opening it
			//Check if it is a storage obj:
			if (storageObj.NextSpareSlot() != null)
			{
				UIManager.TryUpdateSlot(new UISlotObject(storageObj.NextSpareSlot().UUID, interaction.HandObject,
					InventorySlotCache.GetSlotByItem(interaction.HandObject)?.inventorySlot.UUID));
				SoundManager.PlayAtPosition("Rustle0" + UnityEngine.Random.Range(1, 6).ToString(), PlayerManager.LocalPlayer.transform.position);
				ObjectBehaviour itemObj = interaction.HandObject.GetComponent<ObjectBehaviour>();
				itemObj.parentContainer = objectBehaviour;
			}
		}
		else
		{
			//nothing in hand, just open / close the backpack (note that this means backpack can only be moved in inventory
			//by dragging and dropping)
			return Interact(HandActivate.ByLocalPlayer());
		}


		return false;
	}

	public bool Interact(HandActivate interaction)
	{
		//open / close the backpack on activate
		if (UIManager.StorageHandler.storageCache != storageObj)
		{
			UIManager.StorageHandler.OpenStorageUI(storageObj);
		}
		else
		{
			UIManager.StorageHandler.CloseStorageUI();
		}

		return true;
	}

	public void OnPickupServer(HandApply interaction)
	{
		//Do a sync of the storage items when adding to UI
		storageObj.NotifyPlayer(interaction.Performer);
	}


}