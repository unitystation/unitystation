using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Objects.GasContainer;

/// <summary>
/// Component which manages all the equipment on a player.
/// </summary>
public class Equipment : NetworkBehaviour, IExaminable
{
	private Dictionary<NamedSlot, ClothingItem> clothingItems;
	public bool IsInternalsEnabled;
	private ItemSlot maskSlot;
	private ItemStorage itemStorage;
	public ItemStorage ItemStorage => itemStorage;

	private void Awake()
	{
		itemStorage = GetComponent<ItemStorage>();

		clothingItems = new Dictionary<NamedSlot, ClothingItem>();
		foreach (var clothingItem in GetComponentsInChildren<ClothingItem>())
		{
			if (clothingItem.Slot != NamedSlot.none)
			{
				clothingItems.Add(clothingItem.Slot, clothingItem);
			}
		}

		maskSlot = itemStorage.GetNamedItemSlot(NamedSlot.mask);
		InitInternals();
	}

	private void OnDestroy()
	{
		UnregisisterInternals();
	}

	public void NotifyPlayer(NetworkConnection recipient)
	{
		foreach (var clothingItem in clothingItems)
		{
			PlayerAppearanceMessage.SendTo(gameObject, (int) clothingItem.Key, recipient,
				clothingItem.Value.GameObjectReference, true, false);
		}
	}

	public void SetReference(int index, GameObject _Item)
	{
		PlayerAppearanceMessage.SendToAll(gameObject, index, _Item);
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
		var itemAttrs = item.GetComponent<ItemAttributesV2>();
		if (itemAttrs == null) return false;

		if (itemAttrs.HasTrait(CommonTraits.Instance.Mask))
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

	/// <summary>
	/// Gets the clothing item corresponding to this slot, null if no clothing item exists for this slot.
	/// </summary>
	/// <param name="namedSlot"></param>
	/// <returns></returns>
	public ClothingItem GetClothingItem(NamedSlot namedSlot)
	{
		clothingItems.TryGetValue(namedSlot, out var clothingItem);
		return clothingItem;
	}

	/// <summary>
	/// Gets name and jobtype from ID card worn in ID slot if any.
	/// </summary>
	/// <param name="namedSlot"></param>
	/// <returns></returns>
	public String GetIdentityFromID()
	{
		IDCard card = null;
		var tryGetItem = ItemSlot.GetNamed(itemStorage, NamedSlot.id).Item;
		if (tryGetItem != null)
		{
			card = tryGetItem.GetComponent<IDCard>();
		}
		//Logger.Log("ID Card: " + (card != null ? card.ToString() : "null"));
		if (card != null)
		{
			return card.RegisteredName + " " + (card.Occupation ? $" ({card.Occupation.DisplayName})" : "");
		}
		else
		{
			return "";
		}
	}

	public string Examine(Vector3 worldPos)
	{
		// Collect clothing + ID info.
		string msg = "This is " + GetComponent<PlayerScript>().visibleName + ".";

		// TODO: LOOP over items
		// msg += blah;

		return msg;
	}
}
