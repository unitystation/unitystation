using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatchelBehaviour : MonoBehaviour, IServerInventoryMove
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
		storage = GetComponent<ItemStorage>();
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		//was it transferred from a player's visible inventory?
		if (info.FromPlayer != null && holderPlayer != null)
		{
			holderPlayer.PlayerSync.OnTileReached().RemoveListener(TileReachedServer);
			holderPlayer = null;
		}

		if (info.ToPlayer != null)
		{
			if (compatibleSlots.Contains(info.ToSlot.NamedSlot.GetValueOrDefault(NamedSlot.none)))
			{
				info.ToPlayer.PlayerScript.PlayerSync.OnTileReached().AddListener(TileReachedServer);
			}
		}
	}

	private void TileReachedServer(Vector3Int worldPos)
	{
		var crossedItems = MatrixManager.GetAt<Pickupable>(worldPos, true);
		foreach (var item in crossedItems)
		{
			Inventory.ServerAdd(item, storage.GetBestSlotFor(item));
		}
	}
}
