using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Objects;
using UnityEngine;
using Mirror;
using Newtonsoft.Json;

/// <summary>
/// Component which manages all the equipment on a player.
/// </summary>
public class Equipment : MonoBehaviour
{
	private Dictionary<NamedSlot, ClothingItem> clothingItems;
	public bool IsInternalsEnabled;
	private ItemSlot maskSlot;
	private ItemStorage itemStorage;

	private void Awake()
	{
		itemStorage = GetComponent<ItemStorage>();

		clothingItems = new Dictionary<NamedSlot, ClothingItem>();
		foreach (var clothingItem in GetComponentsInChildren<ClothingItem>())
		{
			clothingItems.Add(clothingItem.Slot, clothingItem);
		}
		maskSlot = itemStorage.GetNamedItemSlot(NamedSlot.mask);
		InitInternals();
	}

	private void OnDestroy()
	{
		UnregisisterInternals();
	}

	public void NotifyPlayer(GameObject recipient)
	{
		foreach (var clothingItem in clothingItems)
		{
			EquipmentSpritesMessage.SendTo(gameObject, clothingItem.Key, recipient, clothingItem.Value.GameObjectReference, true, false);
		}
	}

	public void SetReference(int index, GameObject _Item)
	{
		EquipmentSpritesMessage.SendToAll(gameObject, (NamedSlot)index, _Item);
	}

	private void InitInternals()
	{
		IsInternalsEnabled = false;
		EventManager.AddHandler(EVENT.EnableInternals, OnInternalsEnabled);
		EventManager.AddHandler(EVENT.DisableInternals, OnInternalsDisabled);
	}

	/// <summary>
	/// Removes any handlers from the event system
	/// </summary>
	private void UnregisisterInternals()
	{
		EventManager.RemoveHandler(EVENT.EnableInternals, OnInternalsEnabled);
		EventManager.RemoveHandler(EVENT.DisableInternals, OnInternalsDisabled);
	}

	public void OnInternalsEnabled()
	{
		CmdSetInternalsEnabled(true);
	}

	public void OnInternalsDisabled()
	{
		CmdSetInternalsEnabled(false);
	}

	/// <summary>
	/// Clientside method.
	/// Checks if player has proper internals equipment (Oxygen Tank and Mask)
	/// equipped in the correct inventory slots (suitStorage and mask)
	/// </summary>
	public bool HasInternalsEquipped()
	{
		var item = maskSlot?.Item;
		if (item == null) return false;
		var itemAttrs = item.GetComponent<ItemAttributes>();
		if (itemAttrs == null) return false;

		if (itemAttrs.itemType == ItemType.Mask)
		{
			foreach (var gasSlot in itemStorage.GetGasSlots())
			{
				if (gasSlot.Item && gasSlot.Item.GetComponent<GasContainer>())
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Disables or enables the player's internals on the server
	/// </summary>
	/// <param name="internalsEnabled"></param>
	[Command]
	public void CmdSetInternalsEnabled(bool internalsEnabled)
	{
		IsInternalsEnabled = internalsEnabled;
	}

	public ClothingItem GetClothingItem(NamedSlot namedSlot)
	{
		return clothingItems[namedSlot];
	}
}
