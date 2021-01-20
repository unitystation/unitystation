using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "AdminGhostItemPopulator", menuName = "Inventory/Populators/Storage/AdminGhostItemPopulator")]
public class AdminGhostItemPopulator : ItemStoragePopulator
{
	public static List<ItemStorage> ContentsList = new List<ItemStorage>();

	public override void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context)
	{
		foreach (var itemStorage in ContentsList)
		{
			var savedItemSlots = itemStorage.GetItemSlots();
			foreach (var savedSlot in savedItemSlots)
			{
				var ghostSlot = toPopulate.GetNamedItemSlot((NamedSlot)savedSlot.NamedSlot);
				Inventory.ServerTransfer(savedSlot, ghostSlot);
			}
		}
	}
}
