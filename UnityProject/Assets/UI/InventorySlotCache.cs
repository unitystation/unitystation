using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
///     A pseudo-array/dictionary for retrieving inventory slots.
///     Supports multiple interactions to make it easier to access the slot you want to reference.
/// </summary>
/// <example>
///     var beltSlot = inventorySlotCache.BeltSlot;
/// </example>
/// <example>
///     var firstSlot = inventorySlotCache[0];
/// </example>
/// <example>
///     var hatSlot = inventorySlotCache[ItemType.Hat];
/// </example>
/// <example>
///     var idSlot = inventorySlotCache["id"];
/// </example>
/// <example>
///     foreach (var slot in inventorySlotCache)
/// </example>
/// <example>
///     inventorySlotCache.GetSlotByItem(CurrentSlot.Item)
/// </example>
public class InventorySlotCache : MonoBehaviour
{
	public static List<UI_ItemSlot> InventorySlots = new List<UI_ItemSlot>();

	public UI_ItemSlot this [int index] => InventorySlots[index];

	public UI_ItemSlot this [ItemType type] => GetSlotByItemType(type);

	public UI_ItemSlot this [EquipSlot equipSlot] => GetSlotByEvent(equipSlot);

	public int Length => InventorySlots != null ? InventorySlots.Count : 0;

	private void Awake()
	{
		var childSlots = GetComponentsInChildren<UI_ItemSlot>();

		for (int i = 0; i < childSlots.Length; i++)
		{
			if (!InventorySlots.Contains(childSlots[i]))
			{
				InventorySlots.Add(childSlots[i]);
			}
		}
	}

	public ItemType GetItemType(GameObject obj)
	{
		ItemAttributes item = obj.GetComponent<ItemAttributes>();
		return item.itemType;
	}

	public SpriteType GetItemMasterType(GameObject obj)
	{
		ItemAttributes item = obj.GetComponent<ItemAttributes>();
		return item.spriteType;
	}

	/// <summary>
	///     Returns the most fitting slot for a given item to be equipped.
	/// </summary>
	/// <remarks>
	///     Returns the left pocket for non-equippable items.
	/// </remarks>
	public static UI_ItemSlot GetSlotByItemType(GameObject obj)
	{
		ItemAttributes item = obj.GetComponent<ItemAttributes>();
		return GetSlotByItemType(item.itemType);
	}

	public static UI_ItemSlot GetSlotByItem(GameObject obj)
	{
		for (int i = 0; i < InventorySlots.Count; i++)
		{
			if (InventorySlots[i].Item != null)
			{
				if (InventorySlots[i].Item == obj)
				{
					return InventorySlots[i];
				}
			}
		}
		return null;
	}

	public static void Add(UI_ItemSlot item)
	{
		InventorySlots.Add(item);
	}

	public static void Remove(UI_ItemSlot item)
	{
		if (InventorySlots.Contains(item))
		{
			InventorySlots.Remove(item);
		}
	}

	public static UI_ItemSlot GetSlotByItemType(ItemType type)
	{
		EquipSlot eventName = EquipSlot.storage02;
		switch (type)
		{
			case ItemType.Back:
				eventName = EquipSlot.back;
				break;
			case ItemType.Belt:
				eventName = EquipSlot.belt;
				break;
			case ItemType.Ear:
				eventName = EquipSlot.ear;
				break;
			case ItemType.Glasses:
				eventName = EquipSlot.eyes;
				break;
			case ItemType.Gloves:
				eventName = EquipSlot.hands;
				break;
			case ItemType.Hat:
				eventName = EquipSlot.head;
				break;
			case ItemType.ID:
				eventName = EquipSlot.id;
				break;
			case ItemType.PDA:
				eventName = EquipSlot.id;
				break;
			case ItemType.Mask:
				eventName = EquipSlot.mask;
				break;
			case ItemType.Neck:
				eventName = EquipSlot.neck;
				break;
			case ItemType.Shoes:
				eventName = EquipSlot.feet;
				break;
			case ItemType.Suit:
				eventName = EquipSlot.exosuit;
				break;
			case ItemType.Uniform:
				eventName = EquipSlot.uniform;
				break;
			case ItemType.Gun:
				eventName = EquipSlot.suitStorage;
				break;
		}

		return GetSlotByEvent(eventName);
	}

	public static UI_ItemSlot GetSlotByEvent(EquipSlot equipSlot)
	{
		int indexSearch = InventorySlots.FindIndex(x => x.equipSlot == equipSlot);
		if (indexSearch != -1)
		{
			return InventorySlots[indexSearch];
		}
		else
		{
			return null;
		}
	}
}