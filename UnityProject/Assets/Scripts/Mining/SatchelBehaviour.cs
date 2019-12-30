using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatchelBehaviour : MonoBehaviour, IServerInventoryMove, ICheckedInteractable<HandApply>
{

	private HashSet<NamedSlot> CompatibleSlots = new HashSet<NamedSlot>() {
		NamedSlot.leftHand,
		NamedSlot.rightHand,
		NamedSlot.suitStorage,
		NamedSlot.belt,
	};


	private PlayerScript Player;
	private ItemStorage Storage;

	void Awake()
	{
		Storage = this.GetComponent<ItemStorage>();

	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		//was it transferred from a player's visible inventory?
		if (info.FromPlayer != null && Player != null)
		{
			Player.OnTileReached().RemoveListener(TileReached);
			Player = null;
		}

		if (info.ToPlayer != null)
		{
			if (CompatibleSlots.Contains(info.ToSlot.NamedSlot.GetValueOrDefault(NamedSlot.none)))
			{
				Player = info.ToPlayer.GetComponent<PlayerScript>();
				Player.OnTileReached().AddListener(TileReached);
			}
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (interaction.UsedObject == null) return false;

		if (interaction.TargetObject == this) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		AttemptToStore(interaction.TargetObject);
	}

	public void TileReached(Vector3Int worldPos)
	{
		var crossedItems = MatrixManager.GetAt<ItemAttributesV2>(worldPos, true);
		foreach (var crossedItem in crossedItems)
		{
			AttemptToStore(crossedItem.gameObject);
		}
	}

	private void AttemptToStore(GameObject thing) { 
	
		Pickupable Pickup = thing.GetComponent<Pickupable>();
		if (Pickup != null)
		{
			if (Storage.GetBestSlotFor(Pickup) != null)
			{
				if (Storage.ItemStorageCapacity.CanFit(Pickup, Storage.GetBestSlotFor(Pickup).SlotIdentifier))
				{
					Inventory.ServerAdd(Pickup, Storage.GetBestSlotFor(Pickup), ReplacementStrategy.DropOther);
				}
			}
			var Stack = Pickup.GetComponent<Stackable>();

			if (Stack != null && Stack.Amount > 0)
			{
				if (Storage.GetBestSlotFor(Pickup) != null)
				{
					if (Storage.ItemStorageCapacity.CanFit(Pickup, Storage.GetBestSlotFor(Pickup).SlotIdentifier))
					{
						Inventory.ServerAdd(Pickup, Storage.GetBestSlotFor(Pickup), ReplacementStrategy.DropOther);
					}
				}
			}
		}
	}
}
