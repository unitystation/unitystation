using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
			StoreItem(interaction.Performer, interaction.HandSlot.equipSlot, interaction.HandObject);
		}
		else
		{
			//nothing in hand, just open / close the backpack
			return Interact(HandActivate.ByLocalPlayer());
		}
		return false;
	}

	/// <summary> This is clientside </summary>
	public void StoreItem(GameObject Performer, EquipSlot equipSlot, GameObject item)
	{
		InventorySlot storageInvSlot = storageObj.NextSpareSlot();
		if (storageInvSlot != null && item != gameObject)
		{
			var playerInvSlot = InventoryManager.GetSlotFromOriginatorHand(Performer, equipSlot);
			StoreItemMessage.Send(gameObject, Performer, playerInvSlot.equipSlot, true);
			SoundManager.PlayAtPosition("Rustle0" + Random.Range(1, 6).ToString(), PlayerManager.LocalPlayer.transform.position);
			ObjectBehaviour itemObj = item.GetComponent<ObjectBehaviour>();
			itemObj.parentContainer = objectBehaviour;
		}

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