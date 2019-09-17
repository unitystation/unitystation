using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public partial class PlayerNetworkActions : NetworkBehaviour
{
	private readonly EquipSlot[] playerSlots = {
		EquipSlot.exosuit,
		EquipSlot.belt,
		EquipSlot.feet,
		EquipSlot.head,
		EquipSlot.mask,
		EquipSlot.uniform,
		EquipSlot.neck,
		EquipSlot.ear,
		EquipSlot.eyes,
		EquipSlot.hands,
		EquipSlot.id,
		EquipSlot.back,
		EquipSlot.rightHand,
		EquipSlot.leftHand,
		EquipSlot.storage01,
		EquipSlot.storage02,
		EquipSlot.suitStorage,
		EquipSlot.handcuffs,
	};

	// For access checking. Must be nonserialized.
	// This has to be added because using the UIManager at client gets the server's UIManager. So instead I just had it send the active hand to be cached at server.
	[NonSerialized] public EquipSlot activeHand = EquipSlot.rightHand;


	private bool doingCPR = false;

	private PlayerChatBubble playerChatBubble;

	private Equipment equipment;

	private PlayerMove playerMove;
	private PlayerScript playerScript;

	public Dictionary<EquipSlot, InventorySlot> Inventory { get; } = new Dictionary<EquipSlot, InventorySlot>();


	private void Awake()
	{
		playerMove = GetComponent<PlayerMove>();
		playerScript = GetComponent<PlayerScript>();
		playerChatBubble = GetComponentInChildren<PlayerChatBubble>();
		foreach (var equipSlot in playerSlots)
		{
			var invSlot = new InventorySlot(equipSlot, true, gameObject);
			Inventory.Add(equipSlot, invSlot);
		}
	}

	/// <summary>
	/// Sync the player with the server.
	/// </summary>
	/// <param name="recipient">The player to be synced.</param>
	[Server]
	public void ReenterBodyUpdates(GameObject recipient)
	{
		UpdateInventorySlots();
	}

	/// <summary>
	/// Get the item in the player's active hand
	/// </summary>
	[Server]
	public bool AddItemToUISlot(GameObject itemObject, EquipSlot equipSlot, PlayerNetworkActions originPNA = null, bool replaceIfOccupied = false)
	{
		if (Inventory[equipSlot].Item != null && !replaceIfOccupied)
		{
			return false;
		}

		var cnt = itemObject.GetComponent<CustomNetTransform>();
		if (cnt != null)
		{
			var objectBehaviour = itemObject.GetComponent<ObjectBehaviour>();
			if (objectBehaviour != null)
			{
				objectBehaviour.parentContainer = objectBehaviour;
			}
			cnt.DisappearFromWorldServer();
		}
				if (Inventory[equipSlot].Item != null && !replaceIfOccupied)
		{
			return false;
		}


		if (originPNA != null)
		{
			var fromSlot = InventoryManager.GetSlotFromItem(itemObject, originPNA);
			InventoryManager.ClearInvSlot(fromSlot);
		}
		var toSlot = Inventory[equipSlot];
		InventoryManager.EquipInInvSlot(toSlot, itemObject);

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

	/// Destroys item if it's in player's pool.
	/// It's not recommended to destroy shit in general due to the specifics of our game
	[Server]
	public void Consume(GameObject item)
	{
		foreach (var slot in Inventory)
		{
			if (item == slot.Value.Item)
			{
				InventoryManager.ClearInvSlot(slot.Value);
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
	/// Sends messages to a client to update every slot in the player's clientside inventory.
	/// </summary>
	[Server]
	public void UpdateInventorySlots()
	{
		//for (int i = 0; i < playerSlots.Length; i++)
		//{
		//	InventorySlot inventorySlot = Inventory[playerSlots[i]];
		//	if(inventorySlot.Item != null)
		//	{
		//		if (IsEquipSpriteSlot(fromSlot))
		//		{
		//			if (fromSlot.Item == null)
		//			{
		//				//clear equip sprite
		//				SyncEquipSpritesFor(fromSlot, null);
		//			}
		//		}
		//	}
		//}

		//if (toSlot != null)
		//{
		//	if (toSlot.IsUISlot)
		//	{
		//		if (IsEquipSpriteSlot(toSlot))
		//		{
		//			if (toSlot.Item != null)
		//			{
		//				var att = toSlot.Item.GetComponent<ItemAttributes>();

		//				if (toSlot.SlotName == "leftHand" || toSlot.SlotName == "rightHand")
		//				{
		//					equipment.SetHandItemSprite(att, toSlot.SlotName);
		//				}
		//				else
		//				{
		//					SyncEquipSpritesFor(toSlot, att.gameObject);
		//				}
		//			}
		//		}
		//	}
		//}
	}

	private bool IsEquipSpriteSlot(InventorySlot slot)
	{
		return slot.IsUISlot;
	}

	[Server]
	private void SyncEquipSpritesFor(InventorySlot slot, GameObject Item)
	{
		//clear equip sprite
		//if (slot.Owner.gameObject == gameObject)
		//{
		//	SyncEquipSprite(slot, Item);
		//}
		//else
		//{
		//	slot.Owner.GetComponent<PlayerNetworkActions>()?.SyncEquipSprite(slot, Item);
		//}
	}

	[Server]
	private void SyncEquipSprite(string slotName, GameObject Item)
	{
		EquipSlot enumA = (EquipSlot)Enum.Parse(typeof(EquipSlot), slotName);
		equipment.SetReference((int)enumA, Item);
	}

	/// Drop an item from a slot. use forceSlotUpdate=false when doing cThelientside prediction,
	/// otherwise client will forcefully receive update slot messages
	public void RequestDropItem(string handUUID, bool forceClientInform = true)
	{
		//InventoryInteractMessage.Send("", handUUID, InventoryManager.GetSlotFromUUID(handUUID, isServer).Item, forceClientInform);
	}

	//Dropping from a slot on the UI
	[Server]
	public bool ValidateDropItem(InventorySlot invSlot, bool forceClientInform /* = false*/ )
	{
		//decline if not dropped from hands?
		//if (Inventory.ContainsKey(invSlot.SlotName) && Inventory[invSlot.SlotName].Item)
		//{
		//	DropItem(invSlot.SlotName, forceClientInform);
		//	return true;
		//}

		Logger.Log("Object not found in Inventory", Category.Inventory);
		return false;
	}

	///     Imperative drop.
	/// Pass empty slot to drop a random ones
	[Server]
	public void DropItem(EquipSlot equipSlot)
	{
		InventoryManager.DropItem(Inventory[equipSlot].Item, transform.position, this);
	}

	/// <summary>
	/// Drops all items.
	/// </summary>
	[Server]
	public void DropAll()
	{
		foreach (var key in Inventory.Keys)
		{
			if (Inventory[key].Item)
			{
				DropItem(key);
			}
		}
	}

	/// <summary>
	/// Server handling of the request to drop an item from a client
	/// </summary>
	[Command]
	public void CmdDropItem(EquipSlot equipSlot)
	{
		if (playerScript.canNotInteract() || equipSlot != EquipSlot.leftHand && equipSlot != EquipSlot.rightHand || !SlotNotEmpty(equipSlot))
		{
			return;
		}
		DropItem(equipSlot);
	}

	/// <summary>
	/// Server handling of the request to throw an item from a client
	/// </summary>
	[Command]
	public void CmdThrow(EquipSlot equipSlot, Vector3 worldTargetPos, int aim)
	{
		var inventorySlot = Inventory[equipSlot];
		if (playerScript.canNotInteract() || equipSlot != EquipSlot.leftHand && equipSlot != EquipSlot.rightHand || !SlotNotEmpty(equipSlot))
		{
			return;
		}
		GameObject throwable = inventorySlot.Item;
		Vector3 playerPos = playerScript.PlayerSync.ServerState.WorldPosition;

		InventoryManager.ClearInvSlot(inventorySlot);

		var throwInfo = new ThrowInfo
		{
			ThrownBy = gameObject,
				Aim = (BodyPartType)aim,
				OriginPos = playerPos,
				TargetPos = worldTargetPos,
				//Clockwise spin from left hand and Counterclockwise from the right hand
				SpinMode = equipSlot == EquipSlot.leftHand ? SpinMode.Clockwise : SpinMode.CounterClockwise,
		};
		throwable.GetComponent<CustomNetTransform>().Throw(throwInfo);

		//Simplified counter-impulse for players in space
		if (playerScript.PlayerSync.IsWeightlessServer)
		{
			playerScript.PlayerSync.Push(Vector2Int.RoundToInt(-throwInfo.Trajectory.normalized));
		}
	}

	[Command] //Remember with the parent you can only send networked objects:
	public void CmdPlaceItem(EquipSlot equipSlot, Vector3 pos, GameObject newParent, bool isTileMap)
	{
		if (playerScript.canNotInteract() || !playerScript.IsInReach(pos, true))
		{
			return;
		}

		var inventorySlot = Inventory[equipSlot];
		GameObject item = inventorySlot.Item;

		if (item != null && newParent != null)
		{
			InventoryManager.DropItem(inventorySlot.Item, pos, this);
			if (isTileMap)
			{
				TileChangeManager tileChangeManager = newParent.GetComponentInParent<TileChangeManager>();
				//item.transform.parent = tileChangeManager.ObjectParent.transform; TODO
			}
			else
			{
				item.transform.parent = newParent.transform;
			}
			// TODO
			//ReorderGameobjectsOnTile(pos);
		}
	}

	[Command]
	public void CmdUpdateSlot(EquipSlot target, EquipSlot from)
	{
		var formInvSlot = Inventory[from];
		var targetInvSlot = Inventory[target];
		if(UIManager.CanPutItemToSlot(targetInvSlot, formInvSlot.Item, playerScript))
		{
			InventoryManager.EquipInInvSlot(targetInvSlot, formInvSlot.Item);
			InventoryManager.ClearInvSlot(formInvSlot);
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

	public bool SlotNotEmpty(EquipSlot eventName)
	{
		return Inventory.ContainsKey(eventName) && Inventory[eventName].Item != null;
	}

	[Command]
	public void CmdStartMicrowave(EquipSlot equipSlot, GameObject microwave, string mealName)
	{
		Microwave m = microwave.GetComponent<Microwave>();
		m.ServerSetOutputMeal(mealName);
		InventoryManager.ClearInvSlot(Inventory[equipSlot]);
		m.RpcStartCooking();
	}

	[Command]
	public void CmdToggleShutters(GameObject switchObj)
	{
		if (CanInteractWallmount(switchObj.GetComponent<WallmountBehavior>()))
		{
			ShutterSwitch s = switchObj.GetComponent<ShutterSwitch>();
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
			Logger.LogWarningFormat("Player {0} attempted to interact with shutter switch through wall," +
				" this could indicate a hacked client.", Category.Exploits, this.gameObject.name);
		}
	}

	[Command]
	public void CmdToggleLightSwitch(GameObject switchObj)
	{
		if (CanInteractWallmount(switchObj.GetComponent<WallmountBehavior>()))
		{
			LightSwitch s = switchObj.GetComponent<LightSwitch>();
			if (s.isOn == LightSwitch.States.On)
			{
				s.isOn = LightSwitch.States.Off;
			}
			else if (s.isOn == LightSwitch.States.Off) {
				s.isOn = LightSwitch.States.On;
			}

		}
		else
		{
			Logger.LogWarningFormat("Player {0} attempted to interact with light switch through wall," +
				" this could indicate a hacked client.", Category.Exploits, this.gameObject.name);
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
		playerScript.registerTile.IsDownServer = newState != ConsciousState.CONSCIOUS;
		switch (newState)
		{
			case ConsciousState.CONSCIOUS:
				playerMove.allowInput = true;
				playerScript.PlayerSync.SpeedServer = playerMove.RunSpeed;
				break;
			case ConsciousState.BARELY_CONSCIOUS:
				//Drop items when unconscious
				DropItem(EquipSlot.rightHand);
				DropItem(EquipSlot.leftHand);
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
				DropItem(EquipSlot.rightHand);
				DropItem(EquipSlot.leftHand);
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
	public void CmdToggleChatIcon(bool turnOn, string message, ChatChannel chatChannel)
	{
		if (!playerScript.pushPull.VisibleState || (playerScript.mind.jobType == JobType.NULL)
		|| playerScript.playerHealth.IsDead || playerScript.playerHealth.IsCrit)
		{
			//Don't do anything with chat icon if player is invisible or not spawned in
			//This will also prevent clients from snooping other players local chat messages that aren't visible to them
			return;
		}

		RpcToggleChatIcon(turnOn, message, chatChannel);
	}

	[ClientRpc]
	private void RpcToggleChatIcon(bool turnOn, string message, ChatChannel chatChannel)
	{
		if (!playerChatBubble)
		{
			playerChatBubble = GetComponentInChildren<PlayerChatBubble>();
		}

		playerChatBubble.DetermineChatVisual(turnOn, message, chatChannel);
	}

	[Command]
	public void CmdCommitSuicide()
	{
		GetComponent<LivingHealthBehaviour>().ApplyDamage(gameObject, 1000, AttackType.Internal, DamageType.Brute, BodyPartType.Chest);
	}

	//Respawn action for Deathmatch v 0.1.3

	[Command]
	public void CmdRespawnPlayer()
	{
		if (GameManager.Instance.RespawnCurrentlyAllowed)
		{
			SpawnHandler.RespawnPlayer(connectionToClient, playerControllerId, playerScript.mind.jobType, playerScript.characterSettings, gameObject);
			RpcAfterRespawn();
		}
	}

	[Command]
	public void CmdToggleAllowCloning()
	{
		playerScript.mind.DenyCloning = !playerScript.mind.DenyCloning;
	}

	/// <summary>
	/// Spawn the ghost for this player and tell the client to switch input / camera to it
	/// </summary>
	[Command]
	public void CmdSpawnPlayerGhost()
	{
		if(GetComponent<LivingHealthBehaviour>().IsDead)
		{
			var newGhost = SpawnHandler.SpawnPlayerGhost(connectionToClient, playerControllerId, gameObject, playerScript.characterSettings);
			playerScript.mind.Ghosting(newGhost);
		}
	}


	/// <summary>
	/// Asks the server to let the client rejoin into a logged off character.
	/// </summary>
	/// <param name="loggedOffPlayer">The character to be rejoined into.</param>
	[Command]
	public void CmdEnterBody()
	{
		playerScript.mind.StopGhosting();
		var body = playerScript.mind.body.gameObject;
		SpawnHandler.TransferPlayer(connectionToClient, playerControllerId, body, gameObject, EVENT.PlayerSpawned, null);
		body.GetComponent<PlayerScript>().playerNetworkActions.ReenterBodyUpdates(body);
		RpcAfterRespawn();
	}

	/// <summary>
	/// Disables input before a body transfer.
	/// Note this will be invoked on all clients.
	/// </summary>
	[ClientRpc]
	public void RpcBeforeBodyTransfer()
	{
		//no more input can be sent to the body.
		GetComponent<MouseInputController>().enabled = false;
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
	public void CmdEatFood(GameObject food, EquipSlot fromSlot, bool isDrink)
	{
		if (Inventory[fromSlot].Item == null)
		{
			//Already been eaten or the food is no longer in hand
			return;
		}

		Edible baseFood = food.GetComponent<Edible>();
		if (isDrink)
		{
			SoundManager.PlayNetworkedAtPos( "Slurp", transform.position );
		}
		else
		{
			SoundManager.PlayNetworkedAtPos( "EatFood", transform.position );
		}
		PlayerHealth playerHealth = GetComponent<PlayerHealth>();

		//FIXME: remove blood changes after TDM
		//and use this Cmd for healing hunger and applying
		//food related attributes instead:
		playerHealth.bloodSystem.BloodLevel += baseFood.healAmount;
		playerHealth.bloodSystem.StopBleedingAll();

		food.GetComponent<Pickupable>().DisappearObject(Inventory[fromSlot]);

		GameObject leavings = baseFood.leavings;
		if (leavings != null)
		{
			leavings = PoolManager.PoolNetworkInstantiate(leavings);
			AddItemToUISlot(leavings, fromSlot);
		}
	}

	[Command]
	public void CmdSetActiveHand(EquipSlot hand)
	{
		activeHand = hand;
	}

	[Command]
	public void CmdRefillWelder(GameObject welder, GameObject weldingTank)
	{
		//Double check reach just in case:
		if (playerScript.IsInReach(weldingTank, true))
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
		if (Inventory[EquipSlot.leftHand].Item == paper || Inventory[EquipSlot.rightHand].Item == paper)
		{
			var paperComponent = paper.GetComponent<Paper>();
			var pen = Inventory[EquipSlot.leftHand].Item?.GetComponent<Pen>();
			if (pen == null)
			{
				pen = Inventory[EquipSlot.rightHand].Item?.GetComponent<Pen>();
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

	/// <summary>
	/// Performs a hug from one player to another.
	/// </summary>
	[Command]
	public void CmdRequestHug(string hugger, GameObject huggedPlayer)
	{
		string hugged = huggedPlayer.GetComponent<PlayerScript>().playerName;
		var huggedPlayerRegister = huggedPlayer.GetComponent<RegisterPlayer>();
		ChatRelay.Instance.AddToChatLogServer(new ChatEvent
		{
			channels = ChatChannel.Local,
			message = $"{hugger} has hugged {hugged}.",
			position = huggedPlayerRegister.WorldPosition.To2Int()
		});
	}

	/// <summary>
	///	Performs a CPR action from one player to another.
	/// </summary>
	[Command]
	public void CmdRequestCPR(GameObject rescuer, GameObject cardiacArrestPlayer)
	{
		var cardiacArrestPlayerRegister = cardiacArrestPlayer.GetComponent<RegisterPlayer>();

		if (doingCPR)
			return;

		var progressFinishAction = new FinishProgressAction(
			reason =>
			{
				switch (reason)
				{
					case FinishProgressAction.FinishReason.INTERRUPTED:
						CancelCPR();
						doingCPR = false;
						break;
					case FinishProgressAction.FinishReason.COMPLETED:
						DoCPR(rescuer, cardiacArrestPlayer);
						doingCPR = false;
						break;
				}
			}
		);

		doingCPR = true;
		UIManager.ProgressBar.StartProgress(cardiacArrestPlayerRegister.WorldPosition, 5f, progressFinishAction,
			rescuer);
		ChatRelay.Instance.AddToChatLogServer(new ChatEvent
		{
			channels = ChatChannel.Local,
			message = $"{rescuer.Player()?.Name} is trying to perform CPR on {cardiacArrestPlayer.Player()?.Name}.",
			position = cardiacArrestPlayerRegister.WorldPosition.To2Int()
		});
	}

	[Server]
	private void DoCPR(GameObject rescuer, GameObject CardiacArrestPlayer)
	{
		var CardiacArrestPlayerRegister = CardiacArrestPlayer.GetComponent<RegisterPlayer>();
		CardiacArrestPlayer.GetComponent<PlayerHealth>().bloodSystem.oxygenDamage -= 7f;
		doingCPR = false;
		ChatRelay.Instance.AddToChatLogServer(new ChatEvent
		{
			channels = ChatChannel.Local,
			message = $"{rescuer.Player()?.Name} has performed CPR on {CardiacArrestPlayer.Player()?.Name}.",
			position = CardiacArrestPlayerRegister.WorldPositionServer.To2Int()
		});
	}

	[Server]
	private void CancelCPR()
	{
		// Stop the in progress CPR.
		doingCPR = false;
	}

	/// <summary>
	/// Performs a disarm attempt from one player to another.
	/// </summary>
	[Command]
	public void CmdRequestDisarm(GameObject disarmer, GameObject playerToDisarm)
	{
		var rng = new System.Random();
		string disarmerName = disarmer.Player()?.Name;
		string playerToDisarmName = playerToDisarm.Player()?.Name;
		var leftHandSlot = InventoryManager.GetSlotFromOriginatorHand(playerToDisarm, EquipSlot.leftHand);
		var rightHandSlot = InventoryManager.GetSlotFromOriginatorHand(playerToDisarm, EquipSlot.rightHand);
		var disarmedPlayerRegister = playerToDisarm.GetComponent<RegisterPlayer>();
		var disarmedPlayerNetworkActions = playerToDisarm.GetComponent<PlayerNetworkActions>();

		// This is based off the alien/humanoid/attack_hand disarm code of TGStation's codebase.
		// Disarms have 5% chance to knock down, then it has a 50% chance to disarm.
		if (5 >= rng.Next(1, 100))
		{
			disarmedPlayerRegister.Stun(6f, false);
			SoundManager.PlayNetworkedAtPos("ThudSwoosh", disarmedPlayerRegister.WorldPositionServer);
			ChatRelay.Instance.AddToChatLogServer(new ChatEvent
			{
				channels = ChatChannel.Local,
				message = $"{disarmerName} has knocked {playerToDisarmName} down!",
				position = disarmedPlayerRegister.WorldPositionServer.To2Int()
			});
		}
		else if (50 >= rng.Next(1, 100))
		{
			// Disarms
			if (leftHandSlot.Item != null)
			{
				disarmedPlayerNetworkActions.DropItem(EquipSlot.leftHand);
			}

			if (rightHandSlot.Item != null)
			{
				disarmedPlayerNetworkActions.DropItem(EquipSlot.rightHand);
			}

			SoundManager.PlayNetworkedAtPos("ThudSwoosh", disarmedPlayerRegister.WorldPositionServer);
			ChatRelay.Instance.AddToChatLogServer(new ChatEvent
			{
				channels = ChatChannel.Local,
				message = $"{disarmerName} has disarmed {playerToDisarmName}!",
				position = disarmedPlayerRegister.WorldPositionServer.To2Int()
			});
		}
		else
		{
			SoundManager.PlayNetworkedAtPos("PunchMiss", disarmedPlayerRegister.WorldPositionServer);
			ChatRelay.Instance.AddToChatLogServer(new ChatEvent
			{
				channels = ChatChannel.Local,
				message = $"{disarmerName} has attempted to disarm {playerToDisarmName}!",
				position = disarmedPlayerRegister.WorldPositionServer.To2Int()
			});
		}
	}

	//admin only commands
	#region Admin

	[Command]
	public void CmdAdminMakeHotspot(GameObject onObject)
	{
		var reactionManager = onObject.GetComponentInParent<ReactionManager>();
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition(), 700, .05f);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.down, 700, .05f);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.left, 700, .05f);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.up, 700, .05f);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.right, 700, .05f);
	}

	[Command]
	public void CmdAdminSmash(GameObject toSmash)
	{
		toSmash.GetComponent<Integrity>().ApplyDamage(float.MaxValue, AttackType.Melee, DamageType.Brute);
	}

	//simulates despawning and immediately respawning this object, expectation
	//being that it should properly initialize itself regardless of its previous state.
	[Command]
	public void CmdAdminRespawn(GameObject toRespawn)
	{
		PoolManager.PoolNetworkTestDestroyInstantiate(toRespawn);
	}

	#endregion
}
