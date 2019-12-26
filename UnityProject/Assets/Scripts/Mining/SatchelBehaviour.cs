using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatchelBehaviour : MonoBehaviour, IServerInventoryMove
{

	public HashSet<NamedSlot> CompatibleSlots = new HashSet<NamedSlot>() {
		NamedSlot.leftHand,
		NamedSlot.rightHand,
		NamedSlot.suitStorage,
		NamedSlot.belt,
	};


	public PlayerScript Player;
	public ItemStorage Storage;

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

	public void TileReached(Vector3 worldPos)
	{
		MatrixInfo matrix = MatrixManager.AtPoint(worldPos.NormalizeToInt(), true);
		var locPos = matrix.ObjectParent.transform.InverseTransformPoint(worldPos).RoundToInt();
		var crossedItems = matrix.Matrix.Get<ItemAttributesV2>(locPos, true);
		foreach (var crossedItem in crossedItems)
		{
			Pickupable Pickup = crossedItem.GetComponent<Pickupable>();
			if (Pickup != null)
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
