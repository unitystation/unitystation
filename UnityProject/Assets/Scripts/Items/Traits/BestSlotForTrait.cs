
using System;
using System.Collections.Generic;
using System.Linq;
using Items;
using Logs;
using ScriptableObjects;
using UnityEngine;

/// <summary>
/// Singleton SO. When equipping, this SO indicates which item slot is the best slot
/// for the item based on the item's traits.
/// </summary>
[CreateAssetMenu(fileName = "BestSlotForTraitSingleton", menuName = "Singleton/Traits/BestSlotForTrait")]
public class BestSlotForTrait : SingletonScriptableObject<BestSlotForTrait>
{

	[Serializable]
	public class TraitSlotMapping
	{
		[Tooltip("Trait of the item to match on. Leave empty to match all items regardless of their traits.")]
		public ItemTrait Trait;
		public NamedSlot Slot;
	}

	[Tooltip("The item will be checked against the elements of this list in order." +
	         " If the item has the indicated trait or no trait is defined for the entry, it will be checked if it can" +
	         " fit in the indicated named slot. If not, it will continue to check through the" +
	         " list for other possible matches. If no matches are found, it will" +
	         " be placed in an arbitrary available slot.")]
	[ArrayElementTitle("Trait", "Any")]
	[SerializeField] private List<TraitSlotMapping> BestSlots = null;


	/// <summary>
	/// This Named Slots can't be best slots. GetBestSlot will ignore them
	/// </summary>
	private static NamedSlot[] BlackListSlots = new NamedSlot[]{
		NamedSlot.leftHand,
		NamedSlot.rightHand
	};

	/// <summary>
	/// Returns the slot in storage which is the best fit for the item.
	/// The BestSlots list will be scanned through in order. Returns the
	/// first slot in the BestSlots list for which toCheck has the
	/// indicated trait (ignored if trait is left blank) and can be put into the slot. If none of the BestSlots
	/// will fit this item, returns the first slot in storage which can hold the item.
	/// Returns null if the item cannot fit in any slots in storage.
	/// </summary>
	/// <param name="toCheck"></param>
	/// <param name="storage"></param>
	/// <param name="mustHaveUISlot">if true (default), will only return slots
	/// which are linked to a UI slot</param>
	/// <returns></returns>
	public ItemSlot GetBestSlot(Pickupable toCheck, ItemStorage storage, bool mustHaveUISlot = true)
	{
		if (toCheck == null || storage == null)
		{
			Loggy.LogTrace("Cannot get best slot, toCheck or storage was null", Category.PlayerInventory);
			return null;
		}

		var side = CustomNetworkManager.IsServer ? NetworkSide.Server : NetworkSide.Client;
		var itemAttrs = toCheck.GetComponent<ItemAttributesV2>();
		if (itemAttrs == null)
		{
			Loggy.LogTraceFormat("Item {0} has no ItemAttributes, thus it will be put in the" +
			                      " first available slot.", Category.PlayerInventory, toCheck);
		}
		else
		{
			//find the best slot
			var best = BestSlots.FirstOrDefault(tsm =>
				(!mustHaveUISlot || storage.GetNamedItemSlot(tsm.Slot)?.LocalUISlot != null) &&
				Validations.CanFit(storage.GetNamedItemSlot(tsm.Slot), toCheck, side) &&
				(tsm.Trait == null || itemAttrs.HasTrait(tsm.Trait)));
			if (best != null) return storage.GetNamedItemSlot(best.Slot);
		}

		Loggy.LogTraceFormat("Item {0} did not fit in any BestSlots, thus will" +
		                      " be placed in first available slot.", Category.PlayerInventory, toCheck);

		// Get all slots
		var allSlots = storage.GetItemSlots();

		// Filter blaclisted named slots
		var allowedSlots = allSlots.Where((slot) => !slot.NamedSlot.HasValue ||
		(slot.NamedSlot.HasValue && !BlackListSlots.Contains(slot.NamedSlot.Value))).ToArray();

		// Select first avaliable
		return allowedSlots.FirstOrDefault(slot =>
			(!mustHaveUISlot || slot.LocalUISlot != null) &&
			Validations.CanFit(slot, toCheck, side));
	}

	/// <summary>
	/// modified for dynamic storage
	/// </summary>
	/// <param name="toCheck"></param>
	/// <param name="storage"></param>
	/// <param name="mustHaveUISlot"></param>
	/// <returns></returns>
	public ItemSlot GetBestSlot(Pickupable toCheck, DynamicItemStorage storage, bool mustHaveUISlot = true)
	{
		if (toCheck == null || storage == null)
		{
			Loggy.LogTrace("Cannot get best slot, toCheck or storage was null", Category.PlayerInventory);
			return null;
		}

		var side = CustomNetworkManager.IsServer ? NetworkSide.Server : NetworkSide.Client;
		var itemAttrs = toCheck.GetComponent<ItemAttributesV2>();
		if (itemAttrs == null)
		{
			Loggy.LogTraceFormat("Item {0} has no ItemAttributes, thus it will be put in the" +
			                      " first available slot.", Category.PlayerInventory, toCheck);
		}
		else
		{
			//find the best slot
			ItemSlot best = null;
			foreach (var tsm in BestSlots)
			{
				if (mustHaveUISlot)
				{
					bool hasLocalUISlot = false;
					foreach (var itemSlot in storage.GetNamedItemSlots(tsm.Slot))
					{
						if (itemSlot.LocalUISlot != null)
						{
							hasLocalUISlot = true;
						}
					}

					if (hasLocalUISlot == false) continue;
				}

				bool pass = false;
				foreach (var itemSlot in storage.GetNamedItemSlots(tsm.Slot))
				{
					if (Validations.CanFit(itemSlot, toCheck, side))
					{
						best = itemSlot;
						pass = true;
					}
				}
				if (pass == false) continue;

				if (tsm.Trait != null)
				{
					bool thisitemAttrs = itemAttrs.HasTrait(tsm.Trait);
					if (thisitemAttrs == false) continue;
				}
				return best;
			}
		}

		Loggy.LogTraceFormat("Item {0} did not fit in any BestSlots, thus will" +
		                      " be placed in first available slot.", Category.PlayerInventory, toCheck);

		// Get all slots
		var allSlots = storage.GetItemSlots();

		// Filter blaclisted named slots
		var allowedSlots = allSlots.Where((slot) => !slot.NamedSlot.HasValue ||
		                                            (slot.NamedSlot.HasValue && !BlackListSlots.Contains(slot.NamedSlot.Value))).ToArray();

		// Select first avaliable
		return allowedSlots.FirstOrDefault(slot =>
			(!mustHaveUISlot || slot.LocalUISlot != null) &&
			Validations.CanFit(slot, toCheck, side));
	}

}
