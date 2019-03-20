using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public partial class PlayerNetworkActions : NetworkBehaviour
{
	// time that player will spend as a ghost until they respawn
	private const int RESPAWN_TIME_SECONDS = 10;
	private readonly string[] slotNames = {
		"suit",
		"belt",
		"feet",
		"head",
		"mask",
		"uniform",
		"neck",
		"ear",
		"eyes",
		"hands",
		"id",
		"back",
		"rightHand",
		"leftHand",
		"storage01",
		"storage02",
		"suitStorage"
	};

	// For access checking. Must be nonserialized.
	// This has to be added because using the UIManager at client gets the server's UIManager. So instead I just had it send the active hand to be cached at server.
	[NonSerialized] public string activeHand = "rightHand";

	private ChatIcon chatIcon;

	private Equipment equipment;
	private PlayerMove playerMove;
	private PlayerScript playerScript;
	private UserControlledSprites playerSprites;
	private ObjectBehaviour objectBehaviour;

	public Dictionary<string, InventorySlot> Inventory { get; } = new Dictionary<string, InventorySlot>();

	private static readonly Vector3 FALLEN = new Vector3(0, 0, -90);
	private static readonly Vector3 STRAIGHT = Vector3.zero;

	private void Start()
	{
		equipment = GetComponent<Equipment>();
		playerMove = GetComponent<PlayerMove>();
		playerSprites = GetComponent<UserControlledSprites>();
		playerScript = GetComponent<PlayerScript>();
		chatIcon = GetComponentInChildren<ChatIcon>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}

	public override void OnStartServer()
	{
		if (isServer)
		{
			if (playerScript == null)
			{
				playerScript = GetComponent<PlayerScript>();
			}
			List<InventorySlot> initSync = new List<InventorySlot>();
			foreach (string slotName in slotNames)
			{
				var invSlot = new InventorySlot(Guid.NewGuid(), slotName, true, playerScript);
				Inventory.Add(slotName, invSlot);
				InventoryManager.AllServerInventorySlots.Add(invSlot);
				initSync.Add(invSlot);
			}

			SyncPlayerInventoryGuidMessage.Send(gameObject, initSync);

			//if this is the ghost, respawn after 10 seconds
			if (playerScript.IsGhost)
			{
				RespawnPlayer(RESPAWN_TIME_SECONDS);
			}
		}

		base.OnStartServer();
	}



	public bool InventoryContainsItem(GameObject item, out InventorySlot slot)
	{
		foreach (KeyValuePair<string, InventorySlot> entry in Inventory)
		{
			if (entry.Value.Item == item)
			{
				slot = entry.Value;
				return true;
			}
		}
		slot = null;
		return false;
	}

	[Server]
	public bool AddItemToUISlot(GameObject itemObject, string slotName, bool replaceIfOccupied = false, bool forceInform = true)
	{
		if (Inventory[slotName] == null)
		{
			return false;
		}
		if (Inventory[slotName].Item != null && !replaceIfOccupied)
		{
			Logger.Log($"{gameObject.name}: Didn't replace existing {slotName} item {Inventory[slotName].Item?.name} with {itemObject?.name}", Category.Inventory);
			return false;
		}

		ObjectBehaviour itemObj = itemObject.GetComponent<ObjectBehaviour>();
		var cnt = itemObject.GetComponent<CustomNetTransform>();
		if (cnt != null)
		{
			if (itemObj != null)
			{
				itemObj.parentContainer = objectBehaviour;
			}
			cnt.DisappearFromWorldServer();
		}

		SetInventorySlot(slotName, itemObject);
		return true;
	}

	/// <summary>
	/// Get the item in the player's active hand
	/// </summary>
	/// <returns>the gameobject item in the player's active hand, null if nothing in active hand</returns>
	public GameObject GetActiveHandItem()
	{
		return Inventory[activeHand].Item;
	}

	private void PlaceInHand(GameObject item)
	{
		UIManager.Hands.CurrentSlot.SetItem(item);
	}

	/// Destroys item if it's in player's pool.
	/// It's not recommended to destroy shit in general due to the specifics of our game
	[Server]
	public void Consume(GameObject item)
	{
		foreach (var slot in Inventory)
		{
			if (item == slot.Value.Item)
			{
				InventoryManager.DestroyItemInSlot(item);
				ClearInventorySlot(slot.Key);
				break;
			}
		}
	}

	/// Checks if player has this item in any of his slots
	[Server]
	public bool HasItem(GameObject item)
	{
		foreach (var slot in Inventory)
		{
			if (item == slot.Value.Item)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Validates the inv interaction.
	/// If you are not validating a drop action then pass Vector3.zero to dropWorldPos
	/// </summary>
	[Server]
	public bool ValidateInvInteraction(string slotUUID, string fromUUID, GameObject gObj = null, bool forceClientInform = true)
	{
		//security todo: serverside check for item size UI_ItemSlot.CheckItemFit()
		InventorySlot fromSlot = null;
		InventorySlot toSlot = InventoryManager.GetSlotFromUUID(slotUUID, true);
		if (toSlot == null)
		{
			Logger.Log("Error no slot found for UUID: " + slotUUID, Category.Inventory);
		}
		else
		{
			if (!toSlot.IsUISlot && gObj && InventoryContainsItem(gObj, out fromSlot))
			{
				SetStorageInventorySlot(slotUUID, fromUUID, gObj);
				return true;
			}
			if (toSlot.IsUISlot && gObj && !InventoryContainsItem(gObj, out fromSlot))
			{
				SetInventorySlot(toSlot.SlotName, gObj);
				return true;
			}
			if (toSlot.Item != null)
			{
				if (!toSlot.IsUISlot && toSlot.Item == gObj)
				{
					//It's already been moved to the slot
					fromSlot = InventoryManager.GetSlotFromUUID(fromUUID, isServer);
					if (fromSlot?.Item != null)
					{
						Logger.Log("From slot is not null: " + fromSlot.Item.name +
							" fromItemSlotName: " + fromSlot.UUID, Category.Inventory);
					}
					return true;
				}
				return false;
			}
		}

		if (!gObj)
		{
			return ValidateDropItem(toSlot, forceClientInform);
		}

		if (toSlot.IsUISlot && gObj && InventoryContainsItem(gObj, out fromSlot))
		{
			SetInventorySlot(toSlot.SlotName, gObj);
			//Clean up other slots
			ClearObjectIfNotInSlot(gObj, fromSlot.SlotName, forceClientInform);
			//			Debug.Log($"Approved moving {gObj.name} to slot {toSlot.SlotName}");
			return true;
		}
		Logger.LogWarning($"Unable to validateInvInteraction {toSlot.SlotName}:{gObj.name}", Category.Inventory);
		return false;
	}

	public void RollbackPrediction(string slotUUID, string fromSlotUUID, GameObject item)
	{
		var toSlotRequest = InventoryManager.GetSlotFromUUID(slotUUID, isServer);
		var slotItCameFrom = InventoryManager.GetSlotFromUUID(fromSlotUUID, isServer);

		if (toSlotRequest != null)
		{
			if (toSlotRequest.Item == item) //it already travelled to slot on the server, send it back for everyone
			{
				if (!ValidateInvInteraction(fromSlotUUID, slotUUID, item, true))
				{
					Logger.LogError("Rollback failed!", Category.Inventory);
				}
				return;
			}
		}

		UpdateSlotMessage.Send(gameObject, fromSlotUUID, slotUUID, item, true);
	}

	[Server]
	private void ClearObjectIfNotInSlot(GameObject gObj, string slot, bool forceClientInform)
	{
		HashSet<string> toBeCleared = new HashSet<string>();
		foreach (string key in Inventory.Keys)
		{
			if (key.Equals(slot) || !Inventory[key].Item)
			{
				continue;
			}

			if (Inventory[key].Equals(gObj))
			{
				toBeCleared.Add(key);
			}
		}

		ClearInventorySlot(forceClientInform, toBeCleared.ToArray());
	}

	[Server]
	public void ClearInventorySlot(params string[] slotNames)
	{
		ClearInventorySlot(true, slotNames);
	}

	[Server]
	private void ClearInventorySlot(bool forceClientInform, params string[] slotNames)
	{
		for (int i = 0; i < slotNames.Length; i++)
		{
			Inventory[slotNames[i]].Item = null;
			equipment.ClearItemSprite(slotNames[i]);
			InventoryManager.UpdateInvSlot(true, null, null, Inventory[slotNames[i]].UUID);
		}

		Logger.LogTraceFormat("Cleared {0}", Category.Inventory, slotNames);
	}

	[Server]
	public void SetStorageInventorySlot(string slotUUID, string fromUUID, GameObject obj)
	{
		InventoryManager.UpdateInvSlot(true, slotUUID, obj,
			fromUUID);

		UpdatePlayerEquipSprites(InventoryManager.GetSlotFromUUID(fromUUID, true),
			InventoryManager.GetSlotFromUUID(slotUUID, true));
	}

	[Server]
	public void SetInventorySlot(string slotName, GameObject obj)
	{
		var fromSlot = InventoryManager.GetSlotFromItem(obj);
		var toSlot = Inventory[slotName];
		InventoryManager.UpdateInvSlot(true, toSlot.UUID, obj,
			fromSlot?.UUID);

		UpdatePlayerEquipSprites(fromSlot, toSlot);
	}

	[Server]
	public void UpdatePlayerEquipSprites(InventorySlot fromSlot, InventorySlot toSlot)
	{
		//Checks both slots and determnes the player equip sprites (only call this after a slot change)
		if (fromSlot != null)
		{
			if (fromSlot.IsUISlot)
			{
				if (IsEquipSpriteSlot(fromSlot))
				{
					if (fromSlot.Item == null)
					{
						//clear equip sprite
						SyncEquipSprite(fromSlot.SlotName, -1);
					}
				}
			}
		}

		if (toSlot != null)
		{
			if (toSlot.IsUISlot)
			{
				if (IsEquipSpriteSlot(toSlot))
				{
					if (toSlot.Item != null)
					{
						var att = toSlot.Item.GetComponent<ItemAttributes>();
						if (toSlot.SlotName == "leftHand" || toSlot.SlotName == "rightHand")
						{
							equipment.SetHandItemSprite(att, toSlot.SlotName);
						}
						else if (att.spriteType == SpriteType.Clothing || att.hierarchy.Contains("headset") ||
							att.hierarchy.Contains("storage/backpack") || att.hierarchy.Contains("storage/bag") ||
							att.hierarchy.Contains("storage/belt") || att.hierarchy.Contains("tank"))
						{
							Epos enumA = (Epos)Enum.Parse(typeof(Epos), toSlot.SlotName);
							equipment.syncEquipSprites[(int)enumA] = att.clothingReference;
						}
					}
				}
			}
		}
	}

	private bool IsEquipSpriteSlot(InventorySlot slot)
	{
		if (slot.SlotName == "id" || slot.SlotName == "storage01" ||
		slot.SlotName == "storage02" || slot.SlotName == "suitStorage")
		{
		return false;
		}
		if (!slot.IsUISlot)
		{
			return false;
		}
		return true;
	}

	[Server]
	private void SyncEquipSprite(string slotName, int spriteRef)
	{
		Epos enumA = (Epos)Enum.Parse(typeof(Epos), slotName);
		equipment.syncEquipSprites[(int)enumA] = spriteRef;
	}

	/// Drop an item from a slot. use forceSlotUpdate=false when doing clientside prediction,
	/// otherwise client will forcefully receive update slot messages
	public void RequestDropItem(string handUUID, bool forceClientInform = true)
	{
		InventoryInteractMessage.Send("", handUUID, InventoryManager.GetSlotFromUUID(handUUID, isServer).Item, forceClientInform);
	}

	//Dropping from a slot on the UI
	[Server]
	public bool ValidateDropItem(InventorySlot invSlot, bool forceClientInform /* = false*/ )
	{
		//decline if not dropped from hands?
		if (Inventory.ContainsKey(invSlot.SlotName) && Inventory[invSlot.SlotName].Item)
		{
			DropItem(invSlot.SlotName, forceClientInform);
			return true;
		}

		Logger.Log("Object not found in Inventory", Category.Inventory);
		return false;
	}

	///     Imperative drop.
	/// Pass empty slot to drop a random one
	[Server]
	public void DropItem(string slot = "", bool forceClientInform = true)
	{
		//Drop random item
		if (slot == "")
		{
			slot = "uniform";
			foreach (var key in Inventory.Keys)
			{
				if (Inventory[key].Item)
				{
					slot = key;
					break;
				}
			}
		}
		InventoryManager.DropGameItem(gameObject, Inventory[slot].Item, transform.position);

		equipment.ClearItemSprite(slot);
	}

	/// <summary>
	/// Drops all items.
	/// </summary>
	[Server]
	public void DropAll()
	{
		//fixme: modified collectionz
		foreach (var key in Inventory.Keys)
		{
			if (Inventory[key].Item)
			{
				DropItem(key);
			}
		}
	}

	/// Client requesting throw to clicked position
	[Command]
	public void CmdRequestThrow(string slot, Vector3 worldTargetPos, int aim)
	{
		if (playerScript.canNotInteract() || slot != "leftHand" && slot != "rightHand" || !SlotNotEmpty(slot))
		{
			RollbackPrediction("", Inventory[slot].UUID, Inventory[slot].Item);
			return;
		}
		GameObject throwable = Inventory[slot].Item;

		Vector3 playerPos = playerScript.PlayerSync.ServerState.WorldPosition;

		InventoryManager.DestroyItemInSlot(throwable);
		ClearInventorySlot(slot);
		var throwInfo = new ThrowInfo
		{
			ThrownBy = gameObject,
				Aim = (BodyPartType)aim,
				OriginPos = playerPos,
				TargetPos = worldTargetPos,
				//Clockwise spin from left hand and Counterclockwise from the right hand
				SpinMode = slot == "leftHand" ? SpinMode.Clockwise : SpinMode.CounterClockwise,
		};
		throwable.GetComponent<CustomNetTransform>().Throw(throwInfo);

		//Simplified counter-impulse for players in space
		if (playerScript.PlayerSync.IsWeightlessServer)
		{
			playerScript.PlayerSync.Push(Vector2Int.RoundToInt(-throwInfo.Trajectory.normalized));
		}
	}

	[Command] //Remember with the parent you can only send networked objects:
	public void CmdPlaceItem(string slotName, Vector3 pos, GameObject newParent, bool isTileMap)
	{
		if (playerScript.canNotInteract() || !playerScript.IsInReach(pos))
		{
			return;
		}

		if (!SlotNotEmpty(slotName))
		{
			return;
		}

		GameObject item = Inventory[slotName].Item;
		InventoryManager.DropGameItem(gameObject, Inventory[slotName].Item, pos);
		ClearInventorySlot(slotName);
		if (item != null && newParent != null)
		{
			if (isTileMap)
			{
				TileChangeManager tileChangeManager = newParent.GetComponentInParent<TileChangeManager>();
				//				item.transform.parent = tileChangeManager.ObjectParent.transform; TODO
			}
			else
			{
				item.transform.parent = newParent.transform;
			}
			// TODO
			//			ReorderGameobjectsOnTile(pos);
		}
	}

	//	private void ReorderGameobjectsOnTile(Vector2 position)
	//	{
	//		List<RegisterItem> items = regCallCmdCrowBarRemoveFloorTileisterTile.Matrix.Get<RegisterItem>(position.RoundToInt()).ToList();
	//
	//		for (int i = 0; i < items.Count; i++)
	//		{
	//			SpriteRenderer sRenderer = items[i].gameObject.GetComponentInChildren<SpriteRenderer>();
	//			if (sRenderer != null)
	//			{
	//				sRenderer.sortingOrder = (i + 1);
	//			}
	//		}
	//	}

	public bool SlotNotEmpty(string eventName)
	{
		return Inventory.ContainsKey(eventName) && Inventory[eventName].Item != null;
	}

	/// Allows interactions if player is in reach or inside closet
	[Command]
	public void CmdToggleCupboard(GameObject cupbObj)
	{
		ClosetControl closet = cupbObj.GetComponent<ClosetControl>();
		if (playerScript.canNotInteract())
		{
			return;
		}
		if (playerScript.IsInReach(cupbObj) || closet.Contains(this.gameObject))
		{
			closet.ServerToggleCupboard();
		}
	}

	[Command]
	public void CmdStartMicrowave(string slotName, GameObject microwave, string mealName)
	{
		Microwave m = microwave.GetComponent<Microwave>();
		m.ServerSetOutputMeal(mealName);
		ClearInventorySlot(slotName);
		m.RpcStartCooking();
	}

	[Command]
	public void CmdToggleShutters(GameObject switchObj)
	{
		if (CanInteractWallmount(switchObj.GetComponent<WallmountBehavior>()))
		{
			ShutterSwitchTrigger s = switchObj.GetComponent<ShutterSwitchTrigger>();
			if (s.IsClosed)
			{
				s.IsClosed = false;
			}
			else
			{
				s.IsClosed = true;
			}
		}
		else
		{
			Logger.LogWarning("player attempted to interact with shutter switch through wall," +
				" this could indicate a hacked client.");
		}
	}

	[Command]
	public void CmdToggleLightSwitch(GameObject switchObj)
	{
		if (CanInteractWallmount(switchObj.GetComponent<WallmountBehavior>()))
		{
			LightSwitchTrigger s = switchObj.GetComponent<LightSwitchTrigger>();
			s.isOn = !s.isOn;
		}
		else
		{
			Logger.LogWarning("player attempted to interact with light switch through wall," +
				" this could indicate a hacked client.");
		}
	}

	[Command]
	public void CmdToggleFireCabinet(GameObject cabObj, bool forItemInteract, string currentSlotName)
	{
		if (CanInteractWallmount(cabObj.GetComponent<WallmountBehavior>()))
		{
			FireCabinetTrigger c = cabObj.GetComponent<FireCabinetTrigger>();

			if (!forItemInteract)
			{
				if (c.IsClosed)
				{
					c.IsClosed = false;
				}
				else
				{
					c.IsClosed = true;
				}
			}
			else
			{
				if (c.isFull)
				{
					c.isFull = false;
					if (AddItemToUISlot(c.storedObject.gameObject, currentSlotName))
					{
						c.storedObject.visibleState = true;
						c.storedObject = null;
					}
				}
				else
				{
					c.storedObject = Inventory[currentSlotName].Item.GetComponent<ObjectBehaviour>();
					ClearInventorySlot(currentSlotName);
					c.storedObject.visibleState = false;
					c.isFull = true;
				}
			}
		}
		else
		{
			Logger.LogWarning("player attempted to interact with fire cabinet through wall," +
				" this could indicate a hacked client.");
		}
	}

	[Command]
	public void CmdMoveItem(GameObject item, Vector3 newPos)
	{
		item.transform.position = newPos;
	}

	/// <summary>
	/// Validates that the player can interact with the specified wallmount
	/// </summary>
	/// <param name="wallmount">wallmount to check</param>
	/// <returns>true iff interaction is allowed</returns>
	[Server]
	private bool CanInteractWallmount(WallmountBehavior wallmount)
	{
		//can only interact if the player is facing the wallmount
		return wallmount.IsFacingPosition(transform.position);
	}

	/// <summary>
	/// Process the effects of a conscious state being changed (invoked from PlayerHealth on server when
	/// conscious state changes)
	/// </summary>
	/// <param name="oldState"></param>
	/// <param name="newState"></param>
	[Server]
	public void OnConsciousStateChanged(ConsciousState oldState, ConsciousState newState)
	{
		switch (newState)
		{
			case ConsciousState.CONSCIOUS:
				playerMove.allowInput = true;
				playerScript.PlayerSync.SpeedServer = playerMove.RunSpeed;
				break;
			case ConsciousState.BARELY_CONSCIOUS:
				//Drop items when unconscious
				DropItem("rightHand");
				DropItem("leftHand");
				playerMove.allowInput = true;
				playerScript.PlayerSync.SpeedServer = playerMove.CrawlSpeed;
				if (oldState == ConsciousState.CONSCIOUS)
				{
					//only play the sound if we are falling
					SoundManager.PlayNetworkedAtPos( "Bodyfall", transform.position );
				}
				break;
			case ConsciousState.UNCONSCIOUS:
				//Drop items when unconscious
				DropItem("rightHand");
				DropItem("leftHand");
				playerMove.allowInput = false;
				if (oldState == ConsciousState.CONSCIOUS)
				{
					//only play the sound if we are falling
					SoundManager.PlayNetworkedAtPos( "Bodyfall", transform.position );
				}
				break;
		}
		playerScript.pushPull.CmdStopPulling();
	}

	[Command]
	public void CmdToggleChatIcon(bool turnOn)
	{
		if (!GetComponent<VisibleBehaviour>().visibleState || (playerScript.JobType == JobType.NULL))
		{
			//Don't do anything with chat icon if player is invisible or not spawned in
			return;
		}

		RpcToggleChatIcon(turnOn);
	}

	[ClientRpc]
	private void RpcToggleChatIcon(bool turnOn)
	{
		if (!chatIcon)
		{
			chatIcon = GetComponentInChildren<ChatIcon>();
		}

		if (turnOn)
		{
			chatIcon.TurnOnTalkIcon();
		}
		else
		{
			chatIcon.TurnOffTalkIcon();
		}
	}

	[Command]
	public void CmdCommitSuicide()
	{
		GetComponent<LivingHealthBehaviour>().ApplyDamage(gameObject, 1000, DamageType.Brute, BodyPartType.Chest);
	}

	//Respawn action for Deathmatch v 0.1.3

	[Server]
	public void RespawnPlayer(int timeout = 0)
	{
		if (GameManager.Instance.RespawnAllowed)
		{
			StartCoroutine(InitiateRespawn(timeout));
		}
	}

	/// <summary>
	/// Spawn the ghost for this player and tell the client to switch input / camera to it
	/// </summary>
	[Server]
	public void SpawnPlayerGhost()
	{
		RpcBeforeGhost();
		SpawnHandler.SpawnPlayerGhost(connectionToClient, playerControllerId);

	}


	[Server]
	private IEnumerator InitiateRespawn(int timeout)
	{
		//Debug.LogFormat("{0}: Initiated respawn in {1}s", gameObject.name, timeout);
		yield return new WaitForSeconds(timeout);
		SpawnHandler.RespawnPlayer(connectionToClient, playerControllerId, playerScript.JobType);
		RpcAfterRespawn();
	}

	/// <summary>
	/// Invoked before the ghost is going to be created and input will be shifted to ghost. Allows this body object
	/// to perform needed cleanup. Note this will be invoked on all clients.
	/// </summary>
	/// <param name="bodyObject">object which was turned into a ghost</param>
	[ClientRpc]
	private void RpcBeforeGhost()
	{
		//only need to clean up client side if we are controlling the body who is becoming a ghost
		//no more closet handler, they are dead
		ClosetPlayerHandler cph = GetComponent<ClosetPlayerHandler>();
		if (cph != null)
		{
			Destroy(cph);
		}

		//no more input can be sent to the body.
		MouseInputController mouseInput = GetComponent<MouseInputController>();
		if (mouseInput != null)
		{
			Destroy(mouseInput);
		}
	}

	/// <summary>
	/// Invoked after our respawn is going to be performed by the server. Destroys the ghost.
	/// Note this will be invoked on all clients.
	/// </summary>
	[ClientRpc]
	private void RpcAfterRespawn()
	{
		//this ghost is not needed anymore
		Destroy(gameObject);
	}

	//FOOD
	[Command]
	public void CmdEatFood(GameObject food, string fromSlot, bool isDrink)
	{
		if (Inventory[fromSlot].Item == null)
		{
			//Already been eaten or the food is no longer in hand
			return;
		}

		FoodBehaviour baseFood = food.GetComponent<FoodBehaviour>();
		if (isDrink)
		{
			SoundManager.PlayNetworkedAtPos( "Slurp", transform.position );
		}
		else
		{
			SoundManager.PlayNetworkedAtPos( "EatFood", transform.position );
		}
		PlayerHealth playerHealth = GetComponent<PlayerHealth>();

		//FIXME: remove health and blood changes after TDM
		//and use this Cmd for healing hunger and applying
		//food related attributes instead:
		playerHealth.AddHealth(baseFood.healAmount);
		playerHealth.bloodSystem.BloodLevel += baseFood.healAmount;
		playerHealth.bloodSystem.StopBleeding();

		InventoryManager.UpdateInvSlot(true, "", null, Inventory[fromSlot].UUID);
		equipment.ClearItemSprite(fromSlot);
		PoolManager.Instance.PoolNetworkDestroy(food);

		GameObject leavings = baseFood.leavings;
		if (leavings != null)
		{
			leavings = ItemFactory.SpawnItem(leavings);
			AddItemToUISlot(leavings, fromSlot);
		}
	}

	[Command]
	public void CmdSetActiveHand(string hand)
	{
		activeHand = hand;
	}

	[Command]
	public void CmdRefillWelder(GameObject welder, GameObject weldingTank)
	{
		//Double check reach just in case:
		if (playerScript.IsInReach(weldingTank))
		{
			var w = welder.GetComponent<Welder>();

			//is the welder on?
			if (w.isOn)
			{
				weldingTank.GetComponent<ExplodeWhenShot>().ExplodeOnDamage(gameObject.name);
			}
			else
			{
				//Refuel!
				w.Refuel();
				RpcPlayerSoundAtPos("Refill", transform.position, true);
			}
		}
	}

	[Command]
	public void CmdRequestPaperEdit(GameObject paper, string newMsg)
	{
		//Validate paper edit request
		//TODO Check for Pen
		if (Inventory["leftHand"].Item == paper || Inventory["rightHand"].Item == paper)
		{
			var paperComponent = paper.GetComponent<Paper>();
			var pen = Inventory["leftHand"].Item?.GetComponent<Pen>();
			if (pen == null)
			{
				pen = Inventory["rightHand"].Item?.GetComponent<Pen>();
				if (pen == null)
				{
					//no pen
					paperComponent.UpdatePlayer(gameObject); //force server string to player
					return;
				}
			}

			if (paperComponent != null)
			{
				paperComponent.SetServerString(newMsg);
				paperComponent.UpdatePlayer(gameObject);
			}
		}
	}

}