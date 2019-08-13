using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Objects;
using UnityEngine;
using UnityEngine.Networking;


public class Equipment : NetworkBehaviour
{
	public ClothingItem[] clothingSlots;
	public bool IsInternalsEnabled;

	private PlayerNetworkActions playerNetworkActions;
	private PlayerScript playerScript;
	private InventorySlot[] gasSlots;
	private InventorySlot maskSlot;
	private GameObject idPrefab;

	public NetworkIdentity networkIdentity { get; set; }

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
		playerNetworkActions = gameObject.GetComponent<PlayerNetworkActions>();
		playerScript = gameObject.GetComponent<PlayerScript>();
	}

	private InventorySlot[] InitPotentialGasSlots()
	{
		var slots = new List<InventorySlot>();
		foreach (var slot in playerNetworkActions.Inventory)
		{
			if (GasContainer.GasSlots.Contains(slot.Value.equipSlot))
			{
				slots.Add(slot.Value);
			}
		}

		return slots.ToArray();
	}

	public override void OnStartClient()
	{
		gasSlots = InitPotentialGasSlots();
		maskSlot = playerNetworkActions.Inventory[EquipSlot.mask];
		idPrefab = Resources.Load<GameObject>("ID");
		InitInternals();
	}

	private void OnDestroy()
	{
		UnregisisterInternals();
	}

	public void NotifyPlayer(GameObject recipient)
	{
		for (int i = 0; i < clothingSlots.Length; i++)
		{
			var clothItem = clothingSlots[i];
			EquipmentSpritesMessage.SendTo(gameObject, i, clothItem.reference, recipient, null);
		}
	}

	public void SetPlayerLoadOuts()
	{
		JobOutfit standardOutfit = GameManager.Instance.StandardOutfit.GetComponent<JobOutfit>();
		JobOutfit jobOutfit = GameManager.Instance.GetOccupationOutfit(playerScript.mind.jobType);

		Dictionary<EquipSlot, string> gear = new Dictionary<EquipSlot, string>();

		gear.Add(EquipSlot.uniform, standardOutfit.uniform);
		gear.Add(EquipSlot.ear, standardOutfit.ears);
		gear.Add(EquipSlot.belt, standardOutfit.belt);
		gear.Add(EquipSlot.back, standardOutfit.backpack);
		gear.Add(EquipSlot.feet, standardOutfit.shoes);
		gear.Add(EquipSlot.eyes, standardOutfit.glasses);
		gear.Add(EquipSlot.hands, standardOutfit.gloves);
		gear.Add(EquipSlot.exosuit, standardOutfit.suit);
		gear.Add(EquipSlot.head, standardOutfit.head);
		//gear.Add(EquipSlot.accessory, standardOutfit.accessory);
		gear.Add(EquipSlot.mask, standardOutfit.mask);
		//gear.Add(EquipSlot.backpack, standardOutfit.backpack);
		//gear.Add(EquipSlot.satchel, standardOutfit.satchel);
		//gear.Add(EquipSlot.duffelbag, standardOutfit.duffelbag);
		//gear.Add(EquipSlot.box, standardOutfit.box);
		//gear.Add(EquipSlot.l_hand, standardOutfit.l_hand);
		//gear.Add(EquipSlot.l_pocket, standardOutfit.l_pocket);
		//gear.Add(EquipSlot.r_pocket, standardOutfit.r_pocket);
		//gear.Add(EquipSlot.suit_store, standardOutfit.suit_store);

		if (!string.IsNullOrEmpty(jobOutfit.uniform))
		{
			gear[EquipSlot.uniform] = jobOutfit.uniform;
		}
		/*if (!String.IsNullOrEmpty(jobOutfit.id))
			gear[EquipSlot.id] = jobOutfit.id;*/
		if (!string.IsNullOrEmpty(jobOutfit.ears))
		{
			gear[EquipSlot.ear] = jobOutfit.ears;
		}
		if (!string.IsNullOrEmpty(jobOutfit.belt))
		{
			gear[EquipSlot.belt] = jobOutfit.belt;
		}
		if (!string.IsNullOrEmpty(jobOutfit.backpack))
		{
			gear[EquipSlot.back] = jobOutfit.backpack;
		}
		if (!string.IsNullOrEmpty(jobOutfit.shoes))
		{
			gear[EquipSlot.feet] = jobOutfit.shoes;
		}
		if (!string.IsNullOrEmpty(jobOutfit.glasses))
		{
			gear[EquipSlot.eyes] = jobOutfit.glasses;
		}
		if (!string.IsNullOrEmpty(jobOutfit.gloves))
		{
			gear[EquipSlot.hands] = jobOutfit.gloves;
		}
		if (!string.IsNullOrEmpty(jobOutfit.suit))
		{
			gear[EquipSlot.exosuit] = jobOutfit.suit;
		}
		if (!string.IsNullOrEmpty(jobOutfit.head))
		{
			gear[EquipSlot.head] = jobOutfit.head;
		}
		/*if (!String.IsNullOrEmpty(jobOutfit.accessory))
			gear[EquipSlot.accessory] = jobOutfit.accessory;*/
		if (!string.IsNullOrEmpty(jobOutfit.mask))
		{
			gear[EquipSlot.mask] = jobOutfit.mask;
		}
		/*if (!String.IsNullOrEmpty(jobOutfit.backpack))
			gear[EquipSlot.backpack] = jobOutfit.backpack;
		if (!String.IsNullOrEmpty(jobOutfit.satchel))
			gear[EquipSlot.satchel] = jobOutfit.satchel;
		if (!String.IsNullOrEmpty(jobOutfit.duffelbag))
			gear[EquipSlot.duffelbag] = jobOutfit.duffelbag;
		if (!String.IsNullOrEmpty(jobOutfit.box))
			gear[EquipSlot.box] = jobOutfit.box;
		if (!String.IsNullOrEmpty(jobOutfit.l_hand))
			gear[EquipSlot.l_hand] = jobOutfit.l_hand;
		if (!String.IsNullOrEmpty(jobOutfit.l_pocket))
			gear[EquipSlot.l_pocket] = jobOutfit.l_pocket;
		if (!String.IsNullOrEmpty(jobOutfit.r_pocket))
			gear[EquipSlot.r_pocket] = jobOutfit.r_pocket;
		if (!String.IsNullOrEmpty(jobOutfit.suit_store))
			gear[EquipSlot.suit_store] = jobOutfit.suit_store;*/

		foreach (KeyValuePair<EquipSlot, string> gearItem in gear)
		{
			if (gearItem.Value.Contains(UniItemUtils.ClothingHierIdentifier) || gearItem.Value.Contains(UniItemUtils.HeadsetHierIdentifier) ||
			gearItem.Value.Contains(UniItemUtils.BackPackHierIdentifier) || gearItem.Value.Contains(UniItemUtils.BagHierIdentifier))
			{
				GameObject obj = ClothFactory.CreateCloth(gearItem.Value, TransformState.HiddenPos, transform.parent);
				//if ClothFactory does not return an object then move on to the next clothing item
				if (!obj)
				{
					Logger.LogWarning("Trying to instantiate clothing item " + gearItem.Value + " failed!", Category.Equipment);
					continue;
				}
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(gearItem.Key, itemAtts.gameObject);
			}
			else if (!string.IsNullOrEmpty(gearItem.Value))
			{
				//					Logger.Log(gearItem.Value + " creation not implemented yet.");
			}
		}
		SpawnID(jobOutfit);

		if (playerScript.mind.jobType == JobType.SYNDICATE)
		{
			//Check to see if there is a nuke and communicate the nuke code:
			Nuke nuke = FindObjectOfType<Nuke>();
			if (nuke != null)
			{
				UpdateChatMessage.Send(gameObject, ChatChannel.Syndicate,
													"We have intercepted the code for the nuclear weapon: " + nuke.NukeCode);
			}
		}
	}

	private void SpawnID(JobOutfit outFit)
	{

		var realName = GetComponent<PlayerScript>().playerName;
		GameObject idObj = PoolManager.PoolNetworkInstantiate(idPrefab, parent: transform.parent);
		if (outFit.jobType == JobType.CAPTAIN)
		{
			idObj.GetComponent<IDCard>().Initialize(IDCardType.captain, outFit.jobType, outFit.allowedAccess, realName);
		}
		else if (outFit.jobType == JobType.HOP || outFit.jobType == JobType.HOS || outFit.jobType == JobType.CMO || outFit.jobType == JobType.RD ||
				 outFit.jobType == JobType.CHIEF_ENGINEER)
		{
			idObj.GetComponent<IDCard>().Initialize(IDCardType.command, outFit.jobType, outFit.allowedAccess, realName);
		}
		else
		{
			idObj.GetComponent<IDCard>().Initialize(IDCardType.standard, outFit.jobType, outFit.allowedAccess, realName);
		}

		SetItem(EquipSlot.id, idObj);
	}

	//To set the actual sprite on the player obj
	public void SetHandItemSprite(ItemAttributes att, EquipSlot hand)
	{
		if (hand == EquipSlot.leftHand)
		{
			SetReference((int)hand, att.NetworkInHandRefLeft(), att.gameObject);
		}
		else
		{
			SetReference((int)hand, att.NetworkInHandRefRight(), att.gameObject);;
		}
		//clothingSlots[enumA].sprites
	}

	public void SetReference(int index, int reference, GameObject _Item)
	{
		EquipmentSpritesMessage.SendToAll(gameObject, index, reference, _Item);
	}

	//
	/// <summary>
	///  Clear any sprite slot by setting the slot to -1 via the slotName (server). If the
	///  specified slot has no associated player sprite, nothing will be done.
	/// </summary>
	/// <param name="slotName">name of the slot (should match an EquipSlot enum)</param>
	public void ClearItemSprite(string slotName)
	{
		EquipSlot enumA = (EquipSlot)Enum.Parse(typeof(EquipSlot), slotName);
		if (HasPlayerSprite(enumA))
		{
			SetReference((int)enumA, -1, null);
		}
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="slot"></param>
	/// <returns>true iff the specified EquipSlot has an associated player sprite.</returns>
	private bool HasPlayerSprite(EquipSlot slot)
	{
		return slot != EquipSlot.id && slot != EquipSlot.storage01 && slot != EquipSlot.storage02 && slot != EquipSlot.suitStorage;
	}

	private void SetItem(EquipSlot slotName, GameObject obj)
	{
		playerNetworkActions.AddItemToUISlot(obj, slotName);
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
		if ( maskSlot?.ItemAttributes?.itemType == ItemType.Mask )
		{
			foreach ( var gasSlot in gasSlots )
			{
				if ( gasSlot.Item && gasSlot.Item.GetComponent<GasContainer>() )
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
}
