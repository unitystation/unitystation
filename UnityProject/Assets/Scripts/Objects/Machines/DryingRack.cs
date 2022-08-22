using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Electricity;
using AddressableReferences;
using Effects;
using Items;
using Machines;
using Objects.Machines;

namespace Objects.Kitchen
{
	public class DryingRack : NetworkBehaviour
	{
		private RegisterTile registerTile;
		private ItemStorage storage;
		public Vector3Int WorldPosition => registerTile.WorldPosition;

		// Checks the first unfilled slot. If the first slot is unfilled then the processor
		// must be empty. (Always adds starting from the first slot, and emptying always
		// empties the entire inventory.)
		public bool IsFilled => (storage.GetNextFreeIndexedSlot().SlotIdentifier.SlotIndex != 0);

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			storage = GetComponent<ItemStorage>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(UpdateMe, 0.5f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		/// <summary>
		/// Count down processor timer.
		/// </summary>
		private void UpdateMe()
		{
			CheckDried(0.5f);
		}

		/// <summary>
		/// Attempts to add an item.
		/// </summary>
		public void RequestAddItem(ItemSlot fromSlot)
		{
			AddItem(fromSlot);
		}

		private void AddItem(ItemSlot fromSlot)
		{
			if (fromSlot == null || fromSlot.IsEmpty || fromSlot.ItemObject.GetComponent<Dryable>() == null) return;

			// If there's a stackable component, add one at a time.
			Stackable stack = fromSlot.ItemObject.GetComponent<Stackable>();
			if (stack == null || stack.Amount == 1)
			{
				Inventory.ServerTransfer(fromSlot, storage.GetNextFreeIndexedSlot());
			}
			else {
				var item = stack.ServerRemoveOne();
				Inventory.ServerAdd(item, storage.GetNextFreeIndexedSlot());
			}
		}
		public void RequestEjectContents()
		{
			EjectContents();
		}

		private void EjectContents()
		{
			storage.ServerDropAll();
		}

		/// <summary>
		/// Dry everything inside it.
		/// </summary>
		private void CheckDried(float dryTime)
		{
			foreach (var slot in storage.GetItemSlots())
			{
				if (slot.IsEmpty == true) break;

				// If, somehow, something undryable is in the drying rack (should be impossible), spit it out.
				var dryable = slot.ItemObject.GetComponent<Dryable>();
				if (dryable == null)
				{
					Inventory.ServerDrop(slot);
					continue;
				}
				if (dryable.AddDryingTime(dryTime) == true)
				{
					// Swap item for its dried version, if applicable.
					if (dryable.DriedProduct == null) return;
					Spawn.ServerPrefab(dryable.DriedProduct, WorldPosition);
					_ = Despawn.ServerSingle(dryable.gameObject);
				}
			}
		}
	}
}
