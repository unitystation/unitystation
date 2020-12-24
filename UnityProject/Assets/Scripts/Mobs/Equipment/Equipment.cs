using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;
using Mirror;
using Objects.Atmospherics;

/// <summary>
/// Component which manages all the equipment on a player.
/// </summary>
public class Equipment : NetworkBehaviour
{
	private PlayerScript script;
	public ItemStorage ItemStorage { get; private set; }

	public bool IsInternalsEnabled;

	private ItemSlot maskSlot;
	private ItemSlot headSlot;
	private ItemSlot idSlot;

	private Dictionary<NamedSlot, ClothingItem> clothingItems;

	[NonSerialized]
	public NamedSlotFlagged obscuredSlots = NamedSlotFlagged.None;

	private string TheyPronoun => script.characterSettings.TheyPronoun();
	private string TheirPronoun => script.characterSettings.TheirPronoun();

	private void Awake()
	{
		ItemStorage = GetComponent<ItemStorage>();
		script = GetComponent<PlayerScript>();

		clothingItems = new Dictionary<NamedSlot, ClothingItem>();
		foreach (var clothingItem in GetComponentsInChildren<ClothingItem>())
		{
			if (clothingItem.Slot != NamedSlot.none)
			{
				clothingItems.Add(clothingItem.Slot, clothingItem);
			}
		}

		maskSlot = ItemStorage.GetNamedItemSlot(NamedSlot.mask);
		headSlot = ItemStorage.GetNamedItemSlot(NamedSlot.head);
		idSlot = ItemStorage.GetNamedItemSlot(NamedSlot.id);

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
			foreach (var gasSlot in ItemStorage.GetGasSlots())
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
	public ClothingItem GetClothingItem(NamedSlot namedSlot)
	{
		clothingItems.TryGetValue(namedSlot, out var clothingItem);
		return clothingItem;
	}

	public bool IsSlotObscured(NamedSlot namedSlot)
	{
		return obscuredSlots.HasFlag(ItemSlot.GetFlaggedSlot(namedSlot));
	}

	#region Identity

	/// <summary>
	/// Determine whether the identity of the player is obscured by articles of clothing
	/// with the ObscuresIdentity trait in the head or mask slots.
	/// </summary>
	public bool IsIdentityObscured()
	{
		// check if any worn mask or headwear obscures identity of the wearer
		return (maskSlot.IsOccupied && maskSlot.Item.TryGetComponent<ClothingV2>(out var mask) && mask.HidesIdentity)
				|| (headSlot.IsOccupied && headSlot.Item.TryGetComponent<ClothingV2>(out var headwear) && headwear.HidesIdentity);
	}

	/// <summary>
	/// Attempts to get the in-game name of the player based on what is in their ID slot.
	/// </summary>
	/// <returns>Unknown if an identity couldn't be found.</returns>
	public string GetPlayerNameByEquipment()
	{
		if (idSlot.IsOccupied && idSlot.Item.TryGetComponent<IDCard>(out var idCard)
				&& string.IsNullOrEmpty(idCard.RegisteredName) == false)
		{
			return idCard.RegisteredName;
		}

		if (idSlot.IsOccupied && idSlot.Item.TryGetComponent<Items.PDA.PDALogic>(out var pda)
				&& string.IsNullOrEmpty(pda.RegisteredPlayerName) == false)
		{
			return pda.RegisteredPlayerName;
		}

		return "Unknown";
	}

	#endregion Identity

	#region Examination

	public string Examine()
	{
		string theyPronoun = TheyPronoun;
		theyPronoun = theyPronoun[0].ToString().ToUpper() + theyPronoun.Substring(1);
		string theirPronoun = TheirPronoun;
		string equipment = "";

		if (IsExaminable(NamedSlot.uniform))
		{
			equipment += $"{theyPronoun} is wearing a <b>{ItemNameInSlot(NamedSlot.uniform)}</b>.\n";
		}

		if (IsExaminable(NamedSlot.head))
		{
			equipment += $"{theyPronoun} is wearing a <b>{ItemNameInSlot(NamedSlot.head)}</b> on {theirPronoun} head.\n";
		}

		if (IsExaminable(NamedSlot.outerwear))
		{
			equipment += $"{theyPronoun} is wearing a <b>{ItemNameInSlot(NamedSlot.outerwear)}</b>.\n";

			if (IsExaminable(NamedSlot.suitStorage))
			{
				equipment += $"{theyPronoun} is carrying a <b>{ItemNameInSlot(NamedSlot.suitStorage)}</b> " +
						$"on {theirPronoun} {ItemNameInSlot(NamedSlot.outerwear)}.\n";
			}
		}

		if (IsExaminable(NamedSlot.back))
		{
			equipment += $"{theyPronoun} has a <b>{ItemNameInSlot(NamedSlot.back)}</b> on {theirPronoun} back.\n";
		}

		if (IsExaminable(NamedSlot.leftHand))
		{
			equipment += $"{theyPronoun} is holding a <b>{ItemNameInSlot(NamedSlot.leftHand)}</b> in {theirPronoun} left hand.\n";
		}

		if (IsExaminable(NamedSlot.rightHand))
		{
			equipment += $"{theyPronoun} is holding a <b>{ItemNameInSlot(NamedSlot.rightHand)}</b> in {theirPronoun} right hand.\n";
		}

		if (IsExaminable(NamedSlot.hands))
		{
			equipment += $"{theyPronoun} has <b>{ItemNameInSlot(NamedSlot.hands)}</b> on {theirPronoun} hands.\n";
		}

		if (IsExaminable(NamedSlot.handcuffs))
		{
			equipment += $"<color=red>{theyPronoun} is restrained with <b>{ItemNameInSlot(NamedSlot.handcuffs)}</b>!</color>\n";
		}

		if (IsExaminable(NamedSlot.belt))
		{
			equipment += $"{theyPronoun} has a <b>{ItemNameInSlot(NamedSlot.belt)}</b> about {theirPronoun} waist.\n";
		}

		if (IsExaminable(NamedSlot.feet))
		{
			equipment += $"{theyPronoun} is wearing <b>{ItemNameInSlot(NamedSlot.feet)}</b> on {theirPronoun} feet.\n";
		}

		if (IsExaminable(NamedSlot.mask))
		{
			equipment += $"{theyPronoun} has a <b>{ItemNameInSlot(NamedSlot.mask)}</b> on {theirPronoun} face.\n";
		}

		if (IsExaminable(NamedSlot.neck))
		{
			equipment += $"{theyPronoun} is wearing a <b>{ItemNameInSlot(NamedSlot.neck)}</b> around {theirPronoun} neck.\n";
		}

		if (IsExaminable(NamedSlot.eyes))
		{
			equipment += $"{theyPronoun} has <b>{ItemNameInSlot(NamedSlot.eyes)}</b> covering {theirPronoun} eyes.\n";
		}

		if (IsExaminable(NamedSlot.ear))
		{
			equipment += $"{theyPronoun} has <b>{ItemNameInSlot(NamedSlot.ear)}</b> on {theirPronoun} ears.\n";
		}

		if (IsExaminable(NamedSlot.id))
		{
			equipment += $"{theyPronoun} is wearing a <b>{ItemNameInSlot(NamedSlot.id)}</b>.\n";
		}

		return equipment;
	}

	private bool IsExaminable(NamedSlot slot)
	{
		if (IsSlotObscured(slot))
		{
			return false;
		}

		var storageSlot = ItemStorage.GetNamedItemSlot(slot);
		if (storageSlot.IsEmpty)
		{
			return false;
		}

		return true;
	}

	private string ItemNameInSlot(NamedSlot slot)
	{
		return ItemStorage.GetNamedItemSlot(slot).ItemObject.ExpensiveName();
	}

	#endregion Examination
}
