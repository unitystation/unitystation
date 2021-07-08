
using System;
using System.Collections.Generic;
using System.Linq;
using Items;
using UnityEngine;

/// <summary>
/// Defines which items can fit in a particular slot based on size and ItemTraits whitelist / blacklist.
/// </summary>
[CreateAssetMenu(fileName = "DefinedSlotCapacity", menuName = "Inventory/Structure/DefinedSlotCapacity", order = 4)]
public class DefinedSlotCapacity : SlotCapacity
{
	[SerializeField]
	[Tooltip("Largest item size allowed in this slot")]
	private ItemSize MaxItemSize = ItemSize.Huge;

	[SerializeField]
	[Tooltip("Items with at least one of these traits will be allowed, provided they also have all" +
	         " the required traits. If empty, items will be allowed as long as they" +
	         " don't have blacklisted traits and have all required traits.")]
	private List<ItemTrait> Whitelist = null;

	[SerializeField]
	[Tooltip("Item MUST have ALL of these traits in order to fit.")]
	private List<ItemTrait> Required = null;

	[SerializeField]
	[Tooltip("Items with any of these traits will be disallowed, regardless of if they have the" +
	         " required traits or whitelisted traits (blacklist takes priority over whitelist). " +
	         "If blank, has no effect on capacity logic.")]
	private List<ItemTrait> Blacklist = null;

	/// <summary>
	/// Check if the given item is able to fit in this slot (regardless of whether it is occupied)
	/// </summary>
	/// <param name="toCheck"></param>
	/// <param name="inSlot"></param>
	/// <returns>true iff the item is able to fit</returns>
	public override bool CanFit(Pickupable toCheck)
	{
		if (toCheck == null) return false;
		Logger.LogTraceFormat("Checking if {0} can fit", Category.Inventory, toCheck.name);
		ItemSize size = ItemSize.Huge;
		var itemAttrs = toCheck.GetComponent<ItemAttributesV2>();
		if (itemAttrs != null)
		{
			size = itemAttrs.Size;
		}
		else
		{
			Logger.LogTraceFormat("{0} has no item attrs, defaulting to ItemSize.Huge", Category.Inventory, toCheck.name);
		}

		var sizeLimit = MaxItemSize;
		if (sizeLimit == ItemSize.None)
		{
			Logger.LogTraceFormat("No size restriction defined, defaulting to ItemSize.Huge", Category.Inventory);
			sizeLimit = ItemSize.Huge;
		}

		if (size > sizeLimit)
		{
			Logger.LogTraceFormat("{0} ({1}) exceeds max size of slot ({2})", Category.Inventory, toCheck.name, size, MaxItemSize);
			return false;
		}

		if (Required != null && Required.Count > 0)
		{
			Logger.LogTraceFormat("Requirements are {0}", Category.Inventory,
				String.Join(", ", Required.Select(it => it.name)));
			if (itemAttrs == null)
			{
				Logger.LogTrace("Item has no ItemAttributes, thus cannot meet the requirements ", Category.Inventory);
				return false;
			}
			//requirements are defined, check them
			foreach (var requiredTrait in Required)
			{
				if (!itemAttrs.HasTrait(requiredTrait))
				{
					Logger.LogTraceFormat("Item doesn't have required trait {0}", Category.Inventory, requiredTrait.name);
					return false;
				}
			}
		}

		if (Blacklist != null && Blacklist.Count > 0)
		{
			Logger.LogTraceFormat("Blacklist is {0}", Category.Inventory,
				String.Join(", ", Blacklist.Select(it => it.name)));
			if (itemAttrs == null)
			{
				Logger.LogTrace("Item has no ItemAttributes, thus cannot be blacklisted", Category.Inventory);
			}
			else
			{
				foreach (var blacklistTrait in Blacklist)
				{
					if (itemAttrs.HasTrait(blacklistTrait))
					{
						Logger.LogTraceFormat("Item has blacklisted trait {0}", Category.Inventory, blacklistTrait.name);
						return false;
					}
				}
			}
		}

		if (Whitelist == null || Whitelist.Count == 0)
		{
			Logger.LogTrace("No whitelist defined, item is allowed.", Category.Inventory);
			return true;
		}
		else
		{
			Logger.LogTraceFormat("Whitelist is {0}", Category.Inventory,
				String.Join(", ", Whitelist.Select(it => it == null ? "null" : it.name)));
			if (itemAttrs == null)
			{
				Logger.LogTrace("Item has no ItemAttributes, thus has no whitelisted traits", Category.Inventory);
				return false;
			}
			foreach (var whitelistTrait in Whitelist)
			{
				if (itemAttrs.HasTrait(whitelistTrait))
				{
					Logger.LogTraceFormat("Item has whitelisted trait {0}", Category.Inventory, whitelistTrait.name);
					return true;
				}
			}

			Logger.LogTrace("Item has no whitelisted traits", Category.Inventory);
			return false;
		}
	}
}
