using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatchelBehaviour : MonoBehaviour, IServerInventoryMove, ICheckedInteractable<HandApply>
{

	/// <summary>
	/// slots in which satchel is able to perform its function.
	/// </summary>
	private HashSet<NamedSlot> compatibleSlots = new HashSet<NamedSlot>() {
		NamedSlot.leftHand,
		NamedSlot.rightHand,
		NamedSlot.suitStorage,
		NamedSlot.belt,
	};

	//player currently holding this satchel (if any)
	private PlayerScript holderPlayer;
	private ItemStorage storage;

	void Awake()
	{
		storage = this.GetComponent<ItemStorage>();

	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		//was it transferred from a player's visible inventory?
		if (info.FromPlayer != null && holderPlayer != null)
		{
			holderPlayer.OnTileReached().RemoveListener(TileReachedServer);
			holderPlayer = null;
		}

		if (info.ToPlayer != null)
		{
			if (compatibleSlots.Contains(info.ToSlot.NamedSlot.GetValueOrDefault(NamedSlot.none)))
			{
				holderPlayer = info.ToPlayer.GetComponent<PlayerScript>();
				holderPlayer.OnTileReached().AddListener(TileReachedServer);
			}
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (interaction.UsedObject == null) return false;

		if (interaction.TargetObject == this.gameObject) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		AttemptToStore(interaction.TargetObject);
	}

	private void TileReachedServer(Vector3Int worldPos)
	{
		var crossedItems = MatrixManager.GetAt<ItemAttributesV2>(worldPos, true);
		foreach (var crossedItem in crossedItems)
		{
			AttemptToStore(crossedItem.gameObject);
		}
	}

	private void AttemptToStore(GameObject thing) {

		Pickupable pickup = thing.GetComponent<Pickupable>();
		if (pickup != null)
		{
			if (storage.GetBestSlotFor(pickup) != null)
			{
				if (storage.ItemStorageCapacity.CanFit(pickup, storage.GetBestSlotFor(pickup).SlotIdentifier))
				{
					Inventory.ServerAdd(pickup, storage.GetBestSlotFor(pickup), ReplacementStrategy.DropOther);
				}
			}
			var stack = pickup.GetComponent<Stackable>();

			if (stack != null && stack.Amount > 0)
			{
				if (storage.GetBestSlotFor(pickup) != null)
				{
					if (storage.ItemStorageCapacity.CanFit(pickup, storage.GetBestSlotFor(pickup).SlotIdentifier))
					{
						Inventory.ServerAdd(pickup, storage.GetBestSlotFor(pickup), ReplacementStrategy.DropOther);
					}
				}
			}
		}
	}
}
