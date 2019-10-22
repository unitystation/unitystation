using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Objects;
using UnityEngine;
using Mirror;
using Newtonsoft.Json;

public class Equipment : NetworkBehaviour
{
	public ClothingItem[] clothingSlots;
	public bool IsInternalsEnabled;

	private HeadsetOrPrefab Ears;
	private BackpackOrPrefab Backpack;
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
			EquipmentSpritesMessage.SendTo(gameObject, i, recipient, clothItem.GameObjectReference, true, false);
		}
	}

	public void SetPlayerLoadOuts()
	{
		JobOutfit standardOutfit = GameManager.Instance.StandardOutfit.GetComponent<JobOutfit>();
		JobOutfit jobOutfit = GameManager.Instance.GetOccupationOutfit(playerScript.mind.jobType);

		//Create collection of all the new items to add to the gear slots
		Dictionary<EquipSlot, ClothOrPrefab> gear = new Dictionary<EquipSlot, ClothOrPrefab>();

		gear.Add(EquipSlot.uniform, standardOutfit.uniform);
		gear.Add(EquipSlot.feet, standardOutfit.shoes);
		gear.Add(EquipSlot.eyes, standardOutfit.glasses);
		gear.Add(EquipSlot.hands, standardOutfit.gloves);
		Backpack = standardOutfit.backpack;
		Ears = standardOutfit.ears;
		gear.Add(EquipSlot.exosuit, standardOutfit.suit);
		gear.Add(EquipSlot.head, standardOutfit.head);
		gear.Add(EquipSlot.mask, standardOutfit.mask);
		gear.Add(EquipSlot.suitStorage, standardOutfit.suit_store);

		AddifPresent(gear, EquipSlot.exosuit, jobOutfit.suit);
		AddifPresent(gear, EquipSlot.head, jobOutfit.head);
		AddifPresent(gear, EquipSlot.uniform, jobOutfit.uniform);
		AddifPresent(gear, EquipSlot.feet, jobOutfit.shoes);
		AddifPresent(gear, EquipSlot.hands, jobOutfit.gloves);
		AddifPresent(gear, EquipSlot.eyes, jobOutfit.glasses);
		AddifPresent(gear, EquipSlot.mask, jobOutfit.mask);

		if (jobOutfit.backpack?.Backpack?.Sprites?.Equipped?.Texture != null || jobOutfit.backpack.Prefab != null)
		{
			Backpack = jobOutfit.backpack;
		}

		if (jobOutfit.ears?.Headset?.Sprites?.Equipped?.Texture != null || jobOutfit.ears.Prefab != null)
		{
			Ears = jobOutfit.ears;
		}

		foreach (KeyValuePair<EquipSlot, ClothOrPrefab> gearItem in gear)
		{
			if (gearItem.Value.Clothing != null)
			{
				if (gearItem.Value.Clothing.PrefabVariant != null)
				{
					var obj = ClothFactory.CreateCloth(gearItem.Value.Clothing, TransformState.HiddenPos, transform.parent, PrefabOverride: gearItem.Value.Clothing.PrefabVariant); //Where it is made
					ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
					SetItem(gearItem.Key, itemAtts.gameObject, true);
				}
				else {
					var obj = ClothFactory.CreateCloth(gearItem.Value.Clothing, TransformState.HiddenPos, transform.parent);
					ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
					SetItem(gearItem.Key, itemAtts.gameObject, true);
				}
			}
			else if (gearItem.Value.Prefab != null)
			{
				var obj = PoolManager.PoolNetworkInstantiate(gearItem.Value.Prefab, TransformState.HiddenPos, transform.parent);
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(gearItem.Key, itemAtts.gameObject, true);
			}
		}
		if (Backpack.Backpack != null)
		{
			if (Backpack.Backpack.PrefabVariant != null)
			{
				var obj = ClothFactory.CreateBackpackCloth(Backpack.Backpack, TransformState.HiddenPos, transform.parent, PrefabOverride: Backpack.Backpack.PrefabVariant); //Where it is made
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(EquipSlot.back, itemAtts.gameObject, true);
			}
			else {
				var obj = ClothFactory.CreateBackpackCloth(Backpack.Backpack, TransformState.HiddenPos, transform.parent);
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(EquipSlot.back, itemAtts.gameObject, true);
			}
		}
		else if (Backpack.Prefab)
		{
			var obj = PoolManager.PoolNetworkInstantiate(Backpack.Prefab, TransformState.HiddenPos, transform.parent);
			ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
			SetItem(EquipSlot.back, itemAtts.gameObject, true);
		}


		if (Ears.Headset != null)
		{
			if (Ears.Headset.PrefabVariant != null)
			{
				var obj = ClothFactory.CreateHeadsetCloth(Ears.Headset, TransformState.HiddenPos, transform.parent, PrefabOverride: Ears.Headset.PrefabVariant); //Where it is made
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(EquipSlot.ear, itemAtts.gameObject, true);
			}
			else {
				var obj = ClothFactory.CreateHeadsetCloth(Ears.Headset, TransformState.HiddenPos, transform.parent);
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(EquipSlot.ear, itemAtts.gameObject, true);
			}
		}
		else if (Ears.Prefab)
		{
			var obj = PoolManager.PoolNetworkInstantiate(Backpack.Prefab, TransformState.HiddenPos, transform.parent);
			ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
			SetItem(EquipSlot.ear, itemAtts.gameObject, true);
		}
		SpawnID(jobOutfit);

		CheckForSpecialRoleTypes();
	}

	/// <summary>
	/// Special set up instructions when spawning as a special role
	/// </summary>
	void CheckForSpecialRoleTypes()
	{
		if (playerScript.mind.jobType == JobType.SYNDICATE)
		{
			//Check to see if there is a nuke and communicate the nuke code:
			Nuke nuke = FindObjectOfType<Nuke>();
			if (nuke != null)
			{
				UpdateChatMessage.Send(gameObject, ChatChannel.Syndicate, ChatModifier.None,
					"We have intercepted the code for the nuclear weapon: " + nuke.NukeCode);
			}
		}
	}

	/// <summary>
	/// Does it have SpriteSheetData for Equiped sprites or does it contain a prefab?
	/// Else don't spawn it
	/// </summary>
	private void AddifPresent(Dictionary<EquipSlot, ClothOrPrefab> gear, EquipSlot key, ClothOrPrefab clothOrPrefab)
	{
		if (clothOrPrefab?.Clothing?.Base?.Equipped != null || clothOrPrefab?.Prefab != null)
		{
			gear[key] = clothOrPrefab;
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

	public void SetReference(int index, GameObject _Item)
	{
		EquipmentSpritesMessage.SendToAll(gameObject, index, _Item);
	}

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
			SetReference((int)enumA, null);
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

	private void SetItem(EquipSlot slotName, GameObject obj, bool isInit = false)
	{
		playerNetworkActions.AddItemToUISlot(obj, slotName, null, false, isInit);
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
		if (maskSlot?.ItemAttributes?.itemType == ItemType.Mask)
		{
			foreach (var gasSlot in gasSlots)
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
}
