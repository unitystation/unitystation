using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class Equipment : NetworkBehaviour
{
	public ClothingItem[] clothingSlots;

	private bool isInit;
	private PlayerNetworkActions playerNetworkActions;
	private PlayerScript playerScript;
	public SyncListInt syncEquipSprites = new SyncListInt();

	public NetworkIdentity networkIdentity { get; set; }

	private void Start()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
		playerNetworkActions = gameObject.GetComponent<PlayerNetworkActions>();
		playerScript = gameObject.GetComponent<PlayerScript>();
	}

	public override void OnStartServer()
	{
		InitEquipment();
		base.OnStartServer();
	}

	public override void OnStartClient()
	{
		InitEquipment();
		base.OnStartClient();
	}

	private void InitEquipment()
	{
		if (isInit)
		{
			return;
		}

		syncEquipSprites.Callback = SyncSprites;
		for (int i = 0; i < clothingSlots.Length; i++)
		{
			//All the other slots:
			clothingSlots[i].Reference = -1;
			if (isServer)
			{
				syncEquipSprites.Add(-1);
			}
			else
			{
				clothingSlots[i].Reference = syncEquipSprites[i];
			}
		}
		isInit = true;
		if (isServer)
		{
			StartCoroutine(SetPlayerLoadOuts());
		}
	}

	public void SyncSprites(SyncList<int>.Operation op, int index)
	{
		clothingSlots[index].Reference = syncEquipSprites[index];
	}

	/// Wait until client gains control of this player before proceeding further
	/// (Could be moved into some more generic place in the future)
	private IEnumerator WaitUntilInControl(int maxTries = 50)
	{
		int tries = 0;
		while (!PlayerList.Instance.ContainsGameObject(gameObject))
		{
			if (tries++ > maxTries)
			{
				Logger.LogError($"{this} not in control after {maxTries} tries", Category.Equipment);
				yield break;
			}

			yield return YieldHelper.DeciSecond;
		}
	}


	public IEnumerator SetPlayerLoadOuts()
	{
		//Waiting for player name resolve
		yield return WaitUntilInControl();

		// Null Job players dont get a loadout
		if (playerScript.JobType == JobType.NULL)
		{
			yield break;
		}

		PlayerScript pS = GetComponent<PlayerScript>();
		pS.JobType = playerScript.JobType;

		JobOutfit standardOutfit = GameManager.Instance.StandardOutfit.GetComponent<JobOutfit>();
		JobOutfit jobOutfit = GameManager.Instance.GetOccupationOutfit(playerScript.JobType);

		Dictionary<string, string> gear = new Dictionary<string, string>();

		gear.Add("uniform", standardOutfit.uniform);
		gear.Add("ears", standardOutfit.ears);
		gear.Add("belt", standardOutfit.belt);
		gear.Add("back", standardOutfit.backpack);
		gear.Add("shoes", standardOutfit.shoes);
		gear.Add("glasses", standardOutfit.glasses);
		gear.Add("gloves", standardOutfit.gloves);
		gear.Add("suit", standardOutfit.suit);
		gear.Add("head", standardOutfit.head);
		//gear.Add("accessory", standardOutfit.accessory);
		gear.Add("mask", standardOutfit.mask);
		//gear.Add("backpack", standardOutfit.backpack);
		//gear.Add("satchel", standardOutfit.satchel);
		//gear.Add("duffelbag", standardOutfit.duffelbag);
		//gear.Add("box", standardOutfit.box);
		//gear.Add("l_hand", standardOutfit.l_hand);
		//gear.Add("l_pocket", standardOutfit.l_pocket);
		//gear.Add("r_pocket", standardOutfit.r_pocket);
		//gear.Add("suit_store", standardOutfit.suit_store);

		if (!string.IsNullOrEmpty(jobOutfit.uniform))
		{
			gear["uniform"] = jobOutfit.uniform;
		}
		/*if (!String.IsNullOrEmpty(jobOutfit.id))
			gear["id"] = jobOutfit.id;*/
		if (!string.IsNullOrEmpty(jobOutfit.ears))
		{
			gear["ears"] = jobOutfit.ears;
		}
		if (!string.IsNullOrEmpty(jobOutfit.belt))
		{
			gear["belt"] = jobOutfit.belt;
		}
		if (!string.IsNullOrEmpty(jobOutfit.backpack))
		{
			gear["back"] = jobOutfit.backpack;
		}
		if (!string.IsNullOrEmpty(jobOutfit.shoes))
		{
			gear["shoes"] = jobOutfit.shoes;
		}
		if (!string.IsNullOrEmpty(jobOutfit.glasses))
		{
			gear["glasses"] = jobOutfit.glasses;
		}
		if (!string.IsNullOrEmpty(jobOutfit.gloves))
		{
			gear["gloves"] = jobOutfit.gloves;
		}
		if (!string.IsNullOrEmpty(jobOutfit.suit))
		{
			gear["suit"] = jobOutfit.suit;
		}
		if (!string.IsNullOrEmpty(jobOutfit.head))
		{
			gear["head"] = jobOutfit.head;
		}
		/*if (!String.IsNullOrEmpty(jobOutfit.accessory))
			gear["accessory"] = jobOutfit.accessory;*/
		if (!string.IsNullOrEmpty(jobOutfit.mask))
		{
			gear["mask"] = jobOutfit.mask;
		}
		/*if (!String.IsNullOrEmpty(jobOutfit.backpack))
			gear["backpack"] = jobOutfit.backpack;
		if (!String.IsNullOrEmpty(jobOutfit.satchel))
			gear["satchel"] = jobOutfit.satchel;
		if (!String.IsNullOrEmpty(jobOutfit.duffelbag))
			gear["duffelbag"] = jobOutfit.duffelbag;
		if (!String.IsNullOrEmpty(jobOutfit.box))
			gear["box"] = jobOutfit.box;
		if (!String.IsNullOrEmpty(jobOutfit.l_hand))
			gear["l_hand"] = jobOutfit.l_hand;
		if (!String.IsNullOrEmpty(jobOutfit.l_pocket))
			gear["l_pocket"] = jobOutfit.l_pocket;
		if (!String.IsNullOrEmpty(jobOutfit.r_pocket))
			gear["r_pocket"] = jobOutfit.r_pocket;
		if (!String.IsNullOrEmpty(jobOutfit.suit_store))
			gear["suit_store"] = jobOutfit.suit_store;*/

		foreach (KeyValuePair<string, string> gearItem in gear)
		{
			if (gearItem.Value.Contains(ClothFactory.ClothingHierIdentifier) || gearItem.Value.Contains(ClothFactory.HeadsetHierIdentifier) ||
			gearItem.Value.Contains(ClothFactory.BackPackHierIdentifier) || gearItem.Value.Contains(ClothFactory.BagHierIdentifier))
			{
				GameObject obj = ClothFactory.Instance.CreateCloth(gearItem.Value, TransformState.HiddenPos, transform.parent);
				//if ClothFactory does not return an object then move on to the next clothing item
				if (!obj)
				{
					Logger.LogWarning("Trying to instantiate clothing item " + gearItem.Value + " failed!", Category.Equipment);
					continue;
				}
				ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
				SetItem(GetLoadOutEventName(gearItem.Key), itemAtts.gameObject);
			}
			else if (!string.IsNullOrEmpty(gearItem.Value))
			{
				//					Logger.Log(gearItem.Value + " creation not implemented yet.");
			}
		}
		SpawnID(jobOutfit);

		yield return new WaitForSeconds(3f); //Wait a bit for headset to be fully setup and player to be fully spawned.
		if (playerScript.JobType == JobType.SYNDICATE)
		{
			//Check to see if there is a nuke and communicate the nuke code:
			NukeInteract nuke = FindObjectOfType<NukeInteract>();
			if (nuke != null)
			{
				UpdateChatMessage.Send(gameObject, ChatChannel.Syndicate,
													"We have intercepted the code for the nuclear weapon: " + nuke.NukeCode);
			}
		}
	}

	private void SpawnID(JobOutfit outFit)
	{
		GameObject idObj;
		var realName = PlayerList.Instance.Get(gameObject).Name;
		if (outFit.jobType == JobType.CAPTAIN)
		{
			idObj = ItemFactory.Instance.SpawnIDCard(IDCardType.captain, outFit.jobType, outFit.allowedAccess, realName, transform.parent);
		}
		else if (outFit.jobType == JobType.HOP || outFit.jobType == JobType.HOS || outFit.jobType == JobType.CMO || outFit.jobType == JobType.RD ||
				 outFit.jobType == JobType.CHIEF_ENGINEER)
		{
			idObj = ItemFactory.Instance.SpawnIDCard(IDCardType.command, outFit.jobType, outFit.allowedAccess, realName, transform.parent);
		}
		else
		{
			idObj = ItemFactory.Instance.SpawnIDCard(IDCardType.standard, outFit.jobType, outFit.allowedAccess, realName, transform.parent);
		}

		SetItem("id", idObj);
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

	//To set the actual sprite on the player obj
	public void SetHandItemSprite(ItemAttributes att, string hand)
	{
		Epos enumA = (Epos)Enum.Parse(typeof(Epos), hand);
		if (hand == "leftHand")
		{
			syncEquipSprites[(int)enumA] = att.NetworkInHandRefLeft();
		}
		else
		{
			syncEquipSprites[(int)enumA] = att.NetworkInHandRefRight();
		}
	}

	//
	/// <summary>
	///  Clear any sprite slot by setting the slot to -1 via the slotName (server). If the
	///  specified slot has no associated player sprite, nothing will be done.
	/// </summary>
	/// <param name="slotName">name of the slot (should match an Epos enum)</param>
	public void ClearItemSprite(string slotName)
	{
		Epos enumA = (Epos)Enum.Parse(typeof(Epos), slotName);
		if (hasPlayerSprite(enumA))
		{
			syncEquipSprites[(int)enumA] = -1;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="slot"></param>
	/// <returns>true iff the specified Epos has an associated player sprite.</returns>
	private bool hasPlayerSprite(Epos slot)
	{
		return slot != Epos.id && slot != Epos.storage01 && slot != Epos.storage02 && slot != Epos.suitStorage;
	}

	private void SetItem(string slotName, GameObject obj)
	{
		StartCoroutine(SetItemPatiently(slotName, obj));

		/*			if (String.IsNullOrEmpty(slotName) || itemAtts == null) {
			return;
			Logger.LogError("Error with item attribute for object: " + itemAtts.gameObject.name);
		}

		EquipmentPool.AddGameObject(gameObject, itemAtts.gameObject);

		playerNetworkActions.TrySetItem(slotName, itemAtts.gameObject);
		//Sync all clothing items across network using SyncListInt syncEquipSprites
		if (itemAtts.spriteType == SpriteType.Clothing)
		{
			Epos enumA = (Epos)Enum.Parse(typeof(Epos), slotName);
			syncEquipSprites[(int)enumA] = itemAtts.clothingReference;
		}*/
	}

	private IEnumerator SetItemPatiently(string slotName, GameObject obj)
	{
		//Waiting for hier name resolve
		yield return new WaitForSeconds(0.2f);
		playerNetworkActions.AddItemToUISlot(obj, slotName, true);
	}
}
