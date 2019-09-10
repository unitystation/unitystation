using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Objects;
using UnityEngine;
using UnityEngine.Networking;
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
			EquipmentSpritesMessage.SendTo(gameObject, i, recipient, clothItem.GameObjectReference);
		}
	}

	public void SetPlayerLoadOuts()
	{
		JobOutfit standardOutfit = GameManager.Instance.StandardOutfit.GetComponent<JobOutfit>();
		JobOutfit jobOutfit = GameManager.Instance.GetOccupationOutfit(playerScript.mind.jobType);

		Dictionary<EquipSlot, ClothOrPrefab> gear = new Dictionary<EquipSlot, ClothOrPrefab>();
		//Logger.Log("LLLLLLLLLLLLLLLLL > " + JsonConvert.SerializeObject(standardOutfit.CDuniform.Clothing) + " <  " + playerScript.mind.jobType );
		//Logger.Log(standardOutfit.ToString());
		gear.Add(EquipSlot.uniform, standardOutfit.uniform);
		//gear.Add("ears", standardOutfit.ears);
		//gear.Add("belt", standardOutfit.belt);
		gear.Add(EquipSlot.feet, standardOutfit.shoes);
		gear.Add(EquipSlot.eyes, standardOutfit.glasses);
		gear.Add(EquipSlot.hands, standardOutfit.gloves);
		Backpack = standardOutfit.backpack;
		Ears = standardOutfit.ears;
		gear.Add(EquipSlot.exosuit, standardOutfit.suit);
		gear.Add(EquipSlot.head, standardOutfit.head);
		//gear.Add("accessory", standardOutfit.accessory);
		gear.Add(EquipSlot.mask, standardOutfit.mask);
		//gear.Add("backpack", standardOutfit.backpack);
		//gear.Add("satchel", standardOutfit.satchel);
		//gear.Add("duffelbag", standardOutfit.duffelbag);
		//gear.Add("box", standardOutfit.box);
		//gear.Add("l_hand", standardOutfit.l_hand);
		//gear.Add("l_pocket", standardOutfit.l_pocket);
		//gear.Add("r_pocket", standardOutfit.r_pocket);
		gear.Add(EquipSlot.suitStorage, standardOutfit.suit_store);

		//if (!string.IsNullOrEmpty(jobOutfit.uniform))
		//{
		//	gear["uniform"] = jobOutfit.uniform;
		//}
		///*if (!String.IsNullOrEmpty(jobOutfit.id))
		//	gear["id"] = jobOutfit.id;*/
		//if (!string.IsNullOrEmpty(jobOutfit.ears))
		//{
		//	gear["ears"] = jobOutfit.ears;
		//}
		//if (!string.IsNullOrEmpty(jobOutfit.belt))
		//{
		//	gear["belt"] = jobOutfit.belt;
		//}
		//if (!string.IsNullOrEmpty(jobOutfit.backpack))
		//{
		//	gear["back"] = jobOutfit.backpack;
		//}
		AddifPresent(gear, EquipSlot.exosuit, jobOutfit.suit);
		AddifPresent(gear, EquipSlot.head, jobOutfit.head);
		AddifPresent(gear, EquipSlot.uniform, jobOutfit.uniform);
		AddifPresent(gear, EquipSlot.feet, jobOutfit.shoes);
		AddifPresent(gear, EquipSlot.hands, jobOutfit.gloves);
		AddifPresent(gear, EquipSlot.eyes, jobOutfit.glasses);
		AddifPresent(gear, EquipSlot.mask, jobOutfit.mask);
		//AddifPresent(gear, "suit_store", jobOutfit.suit_store);

		//Logger.Log(JsonConvert.SerializeObject(jobOutfit.backpack.Backpack));
		if (jobOutfit.backpack?.Backpack?.Sprites?.Equipped?.Texture != null || jobOutfit.backpack.Prefab != null)
		{
			Backpack = jobOutfit.backpack;
		}

		if (jobOutfit.ears?.Headset?.Sprites?.Equipped?.Texture != null || jobOutfit.ears.Prefab != null)
		{
			Ears = jobOutfit.ears;
		}

		///*if (!String.IsNullOrEmpty(jobOutfit.accessory))
		//	gear["accessory"] = jobOutfit.accessory;*/
		//if (!string.IsNullOrEmpty(jobOutfit.mask))
		//{
		//	gear["mask"] = jobOutfit.mask;
		//}
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
			gear["suit_store"] = jobOutfit.suit_store;*/
		foreach (KeyValuePair<EquipSlot, ClothOrPrefab> gearItem in gear)
		{

			//Logger.Log("RRRRRRRRRRRRR" + JsonConvert.SerializeObject(gearItem.Value.Clothing));
			if (gearItem.Value.Clothing != null)
			{
				if (gearItem.Value.Clothing.PrefabVariant != null)
				{
					var obj = ClothFactory.CreateCloth(gearItem.Value.Clothing, TransformState.HiddenPos, transform.parent, PrefabOverride: gearItem.Value.Clothing.PrefabVariant); //Where it is made
					ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
					SetItem(gearItem.Key, itemAtts.gameObject);
				}
				else {
					var obj = ClothFactory.CreateCloth(gearItem.Value.Clothing, TransformState.HiddenPos, transform.parent); 
					ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
					SetItem(gearItem.Key, itemAtts.gameObject);
				}
			}
			else if (gearItem.Value.Prefab != null)
			{
				var obj = PoolManager.PoolNetworkInstantiate(gearItem.Value.Prefab, TransformState.HiddenPos, transform.parent);
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(gearItem.Key, itemAtts.gameObject);
			}
		}
		if (Backpack.Backpack != null)
		{
			if (Backpack.Backpack.PrefabVariant != null)
			{
				var obj = ClothFactory.CreateBackpackCloth(Backpack.Backpack, TransformState.HiddenPos, transform.parent, PrefabOverride: Backpack.Backpack.PrefabVariant); //Where it is made
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(EquipSlot.back, itemAtts.gameObject);
			}
			else {
				var obj = ClothFactory.CreateBackpackCloth(Backpack.Backpack, TransformState.HiddenPos, transform.parent); 
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(EquipSlot.back, itemAtts.gameObject);
			}
		}
		else if (Backpack.Prefab)
		{
			var obj = PoolManager.PoolNetworkInstantiate(Backpack.Prefab, TransformState.HiddenPos, transform.parent);
			ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
			SetItem(EquipSlot.back, itemAtts.gameObject);
		}


		if (Ears.Headset != null)
		{
			if (Ears.Headset.PrefabVariant != null)
			{
				var obj = ClothFactory.CreateHeadsetCloth(Ears.Headset, TransformState.HiddenPos, transform.parent, PrefabOverride: Ears.Headset.PrefabVariant); //Where it is made
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(EquipSlot.ear, itemAtts.gameObject);
			}
			else {
				var obj = ClothFactory.CreateHeadsetCloth(Ears.Headset, TransformState.HiddenPos, transform.parent); 
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(EquipSlot.ear, itemAtts.gameObject);
			}
		}
		else if (Ears.Prefab)
		{
			var obj = PoolManager.PoolNetworkInstantiate(Backpack.Prefab, TransformState.HiddenPos, transform.parent);
			ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
			SetItem(EquipSlot.ear, itemAtts.gameObject);
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

	// //Hand item sprites after picking up an item (server)
	// public void SetHandItem(string slotName, GameObject obj)
	// {
	// 	ItemAttributes att = obj.GetComponent<ItemAttributes>();
	// 	SetHandItemSprite(att);
	// 	RpcSendMessage(slotName, obj);
	// }

	// [ClientRpc]
	// private void RpcSendMessage(string eventName, GameObject obj)
	// {
	// 	obj.BroadcastMessage("OnAddToInventory", eventName, SendMessageOptions.DontRequireReceiver);
	// }

	public string GetLoadOutEventName(string uniformPosition)
	{
		switch (uniformPosition)
		{
			case "glasses":
				return "eyes";
			case "head":
				return "head";
			case "neck":
				return "neck";
			case "mask":
				return "mask";
			case "ears":
				return "ear";
			case "suit":
				return "suit";
			case "uniform":
				return "uniform";
			case "gloves":
				return "hands";
			case "shoes":
				return "feet";
			case "belt":
				return "belt";
			case "back":
				return "back";
			case "id":
				return "id";
			case "l_pocket":
				return "storage02";
			case "r_pocket":
				return "storage01";
			case "l_hand":
				return "leftHand";
			case "r_hand":
				return "rightHand";
			default:
				Logger.LogWarning("GetLoadOutEventName: Unknown uniformPosition:" + uniformPosition, Category.Equipment);
				return null;
		}
	}

	////To set the actual sprite on the player obj
	//public void SetHandItemSprite(ItemAttributes att, string hand)
	//{
	//	if (hand == EquipSlot.leftHand)
	//	{
	//		SetReference((int)enumA, att.gameObject);
	//	}
	//	else
	//	{
	//		SetReference((int)enumA, att.gameObject);
	//	}
	//	//clothingSlots[enumA].sprites
	//}

	public void SetReference(int index, GameObject _Item)
	{
		//Logger.Log("bob?");
		EquipmentSpritesMessage.SendToAll(gameObject, index, _Item);
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
