using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Items;
using UnityEngine;
using Mirror;
using Objects.Atmospherics;
using Systems.Clothing;
using Messages.Server;

/// <summary>
/// Component which manages all the equipment on a player.
/// </summary>
public class Equipment : NetworkBehaviour
{
	private PlayerScript script;
	public DynamicItemStorage ItemStorage => script.DynamicItemStorage;

	public bool IsInternalsEnabled;

	private Dictionary<NamedSlot, ClothingItem> clothingItems;

	[NonSerialized]
	public NamedSlotFlagged obscuredSlots = NamedSlotFlagged.None;

	private string TheyPronoun => script.characterSettings.TheyPronoun(script);
	private string TheirPronoun => script.characterSettings.TheirPronoun(script);

	private IEnumerable<ItemSlot> maskSlot => ItemStorage.GetNamedItemSlots(NamedSlot.mask);
	private IEnumerable<ItemSlot> headSlot => ItemStorage.GetNamedItemSlots(NamedSlot.head);
	private IEnumerable<ItemSlot> idSlot  => ItemStorage.GetNamedItemSlots(NamedSlot.id);


	private void Awake()
	{
		script = GetComponent<PlayerScript>();

		clothingItems = new Dictionary<NamedSlot, ClothingItem>();
		foreach (var clothingItem in GetComponentsInChildren<ClothingItem>())
		{
			if (clothingItem.Slot != NamedSlot.none)
			{
				clothingItems.Add(clothingItem.Slot, clothingItem);
			}
		}

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
		EventManager.AddHandler(Event.EnableInternals, OnInternalsEnabled);
		EventManager.AddHandler(Event.DisableInternals, OnInternalsDisabled);
	}

	/// <summary>
	/// Removes any handlers from the event system
	/// </summary>
	private void UnregisisterInternals()
	{
		EventManager.RemoveHandler(Event.EnableInternals, OnInternalsEnabled);
		EventManager.RemoveHandler(Event.DisableInternals, OnInternalsDisabled);
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
		return (IsOccupied(maskSlot) && HidesIdentity(maskSlot)
				|| (IsOccupied(headSlot) && HidesIdentity(headSlot)));
	}

	public bool HidesIdentity(IEnumerable<ItemSlot> ToCheck)
	{
		foreach (var itemSlot in ToCheck)
		{
			if (itemSlot.Item.TryGetComponent<ClothingV2>(out var headwear))
			{
				if (headwear.HidesIdentity == false)
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}
		return true;
	}

	public bool IsOccupied(IEnumerable<ItemSlot> ToCheck)
	{
		foreach (var itemSlot in ToCheck)
		{
			if (itemSlot.IsOccupied == false)
			{
				return false;
			}
		}

		return true;
	}


	/// <summary>
	/// Attempts to get the in-game name of the player based on what is in their ID slot.
	/// </summary>
	/// <returns>Unknown if an identity couldn't be found.</returns>
	public string GetPlayerNameByEquipment()
	{
		if (IsOccupied(idSlot))
		{
			foreach (var itemSlot in idSlot)
			{
				if (itemSlot.Item.TryGetComponent<IDCard>(out var idCard))
				{
					if (string.IsNullOrEmpty(idCard.RegisteredName) == false)
					{
						return idCard.RegisteredName;
					}
				}
			}
		}

		if (IsOccupied(idSlot))
		{
			foreach (var itemSlot in idSlot)
			{
				if (itemSlot.Item.TryGetComponent<Items.PDA.PDALogic>(out var pda))
				{
					if (string.IsNullOrEmpty(pda.RegisteredPlayerName) == false)
					{
						return pda.RegisteredPlayerName;
					}
				}
			}
		}

		return "Unknown";
	}

	#endregion Identity

	#region Examination

	public string Examine()
	{

		StringBuilder equipment = new StringBuilder();
		string pronounIs = "";
		string pronounHas = "";
		string theyPronoun = TheyPronoun;
		theyPronoun = theyPronoun[0].ToString().ToUpper() + theyPronoun.Substring(1);
		string theirPronoun = TheirPronoun;

		//switch out words depending on if the examined player is nonbinary, because "they is wearing" is not how you grammer.
		pronounIs = script.characterSettings.IsPronoun(script);
		pronounHas = script.characterSettings.HasPronoun(script);


		if (IsExaminable(NamedSlot.uniform))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.uniform))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounIs} wearing a <b>{ItemNameInSlot(itemSlot)}</b>.\n");
			}
		}

		if (IsExaminable(NamedSlot.head))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.head))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounIs} wearing a <b>{ItemNameInSlot(itemSlot)}</b> on {theirPronoun} head.\n");
			}

		}

		if (IsExaminable(NamedSlot.outerwear))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.outerwear))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounIs} wearing a <b>{ItemNameInSlot(itemSlot)}</b>.\n");
			}
		}

		if (IsExaminable(NamedSlot.suitStorage))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.suitStorage))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounIs} carrying a <b>{ItemNameInSlot(itemSlot)}</b>. \n");
			}
		}

		if (IsExaminable(NamedSlot.back))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.back))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounHas} a <b>{ItemNameInSlot(itemSlot)}</b> on {theirPronoun} back.\n");
			}
		}

		if (IsExaminable(NamedSlot.leftHand))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.leftHand))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounIs} holding a <b>{ItemNameInSlot(itemSlot)}</b> in {theirPronoun} left hand.\n");
			}
		}

		if (IsExaminable(NamedSlot.rightHand))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.rightHand))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounIs} holding a <b>{ItemNameInSlot(itemSlot)}</b> in {theirPronoun} right hand.\n");
			}
		}

		if (IsExaminable(NamedSlot.hands))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.hands))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounHas} <b>{ItemNameInSlot(itemSlot)}</b> on {theirPronoun} hands.\n");
			}
		}

		if (IsExaminable(NamedSlot.handcuffs))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.handcuffs))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"<color=red>{theyPronoun} {pronounIs} restrained with <b>{ItemNameInSlot(itemSlot)}</b>!</color>\n");
			}
		}

		if (IsExaminable(NamedSlot.belt))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.belt))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounHas} a <b>{ItemNameInSlot(itemSlot)}</b> about {theirPronoun} waist.\n");
			}
		}

		if (IsExaminable(NamedSlot.feet))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.feet))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounIs} wearing <b>{ItemNameInSlot(itemSlot)}</b> on {theirPronoun} feet.\n");
			}
		}

		if (IsExaminable(NamedSlot.mask))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.mask))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounHas} a <b>{ItemNameInSlot(itemSlot)}</b> on {theirPronoun} face.\n");
			}
		}

		if (IsExaminable(NamedSlot.neck))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.neck))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounIs} wearing a <b>{ItemNameInSlot(itemSlot)}</b> around {theirPronoun} neck.\n");
			}
		}

		if (IsExaminable(NamedSlot.eyes))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.eyes))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounHas} <b>{ItemNameInSlot(itemSlot)}</b> covering {theirPronoun} eyes.\n");
			}
		}

		if (IsExaminable(NamedSlot.ear))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.ear))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounHas} <b>{ItemNameInSlot(itemSlot)}</b> on {theirPronoun} ears.\n");
			}
		}

		if (IsExaminable(NamedSlot.id))
		{
			foreach (var itemSlot in ItemStorage.GetNamedItemSlots(NamedSlot.id))
			{
				if (itemSlot.IsEmpty)
				{
					continue;
				}
				equipment.Append($"{theyPronoun} {pronounIs} wearing a <b>{ItemNameInSlot(itemSlot)}</b>.\n");
			}
		}

		return equipment.ToString();
	}

	private bool IsExaminable(NamedSlot slot)
	{
		if (IsSlotObscured(slot))
		{
			return false;
		}

		return true;
	}

	private string ItemNameInSlot(ItemSlot slot)
	{
		return slot.ItemObject.ExpensiveName();
	}

	#endregion Examination
}
