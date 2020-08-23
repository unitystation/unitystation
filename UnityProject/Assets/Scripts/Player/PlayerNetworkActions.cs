using System;
using System.Linq;
using System.Text.RegularExpressions;
using AdminTools;
using Audio;
using Items.PDA;
using UnityEngine;
using Mirror;
using UI.PDA;
using Audio.Containers;
using DiscordWebhook;
using InGameEvents;
using ScriptableObjects;
using System.Collections;

public partial class PlayerNetworkActions : NetworkBehaviour
{
	private static readonly StandardProgressActionConfig DisrobeProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Disrobe);

	// For access checking. Must be nonserialized.
	// This has to be added because using the UIManager at client gets the server's UIManager. So instead I just had it send the active hand to be cached at server.
	[NonSerialized] public NamedSlot activeHand = NamedSlot.rightHand;

	private Equipment equipment = null;

	private PlayerMove playerMove;
	private PlayerScript playerScript;
	private ItemStorage itemStorage;

	public Transform chatBubbleTarget;

	private void Awake()
	{
		playerMove = GetComponent<PlayerMove>();
		playerScript = GetComponent<PlayerScript>();
		itemStorage = GetComponent<ItemStorage>();
	}

	/// <summary>
	/// Get the item in the player's slot
	/// </summary>
	/// <returns>the gameobject item in the player's slot, null if nothing </returns>
	public GameObject GetActiveItemInSlot(NamedSlot slot)
	{
		var pu = itemStorage.GetNamedItemSlot(slot).Item;
		return pu?.gameObject;
	}

	/// <summary>
	/// Get the item in the player's active hand
	/// </summary>
	/// <returns>the gameobject item in the player's active hand, null if nothing in active hand</returns>
	public GameObject GetActiveHandItem()
	{
		var pu = itemStorage.GetNamedItemSlot(activeHand).Item;
		return pu?.gameObject;
	}

	/// <summary>
	/// Get the item in the player's off hand
	/// </summary>
	/// <returns>the gameobject item in the player's off hand, null if nothing in off hand</returns>
	public GameObject GetOffHandItem()
	{
		// Get the hand which isn't active
		NamedSlot offHand;
		switch (activeHand)
		{
			case NamedSlot.leftHand:
				offHand = NamedSlot.rightHand;
				break;
			case NamedSlot.rightHand:
				offHand = NamedSlot.leftHand;
				break;
			default:
				Logger.LogError($"{playerScript.playerName} has an invalid activeHand! Found: {activeHand}", Category.Inventory);
				return null;
		}
		var pu = itemStorage.GetNamedItemSlot(offHand).Item;
		return pu?.gameObject;
	}

	/// Checks if player has this item in any of his slots
	[Server]
	public bool HasItem(GameObject item)
	{
		foreach (var slot in itemStorage.GetItemSlotTree())
		{
			if (item == slot.Item?.gameObject)
			{
				return true;
			}
		}

		return false;
	}

	private bool IsEquipSpriteSlot(ItemSlot slot)
	{
		return slot.SlotIdentifier.NamedSlot != null;
	}

	[Server]
	private void SyncEquipSprite(string slotName, GameObject Item)
	{
		NamedSlot enumA = (NamedSlot)Enum.Parse(typeof(NamedSlot), slotName);
		equipment.SetReference((int)enumA, Item);
	}

	/// <summary>
	/// Server handling of the request to perform a resist action.
	/// </summary>
	[Command]
	public void CmdResist()
	{
		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;

		// Handle the movement restricted actions first.
		if (playerScript.playerMove.IsBuckled)
		{
			// Make sure we don't unbuckle if we are currently cuffed.
			if (!playerScript.playerMove.IsCuffed)
			{
				playerScript.playerMove.Unbuckle();
			}
		}
		else if (playerScript.playerHealth.FireStacks > 0) // Check if we are on fire. If we are perform a stop-drop-roll animation and reduce the fire stacks.
		{
			if (!playerScript.registerTile.IsLayingDown)
			{
				// Throw the player down to the floor for 15 seconds.
				playerScript.registerTile.ServerStun(15);
				SoundManager.PlayNetworkedAtPos("Bodyfall", transform.position, sourceObj: gameObject);
			}
			else
			{
				// Remove 5 stacks(?) per roll action.
				playerScript.playerHealth.ChangeFireStacks(-5.0f);
				// Find the next in the roll sequence. Also unlock the facing direction temporarily since ServerStun locks it.
				playerScript.playerDirectional.LockDirection = false;
				Orientation faceDir = playerScript.playerDirectional.CurrentDirection;
				OrientationEnum currentDir = faceDir.AsEnum();

				switch (currentDir)
				{
					case OrientationEnum.Up:
						faceDir = Orientation.Right;
						break;
					case OrientationEnum.Right:
						faceDir = Orientation.Down;
						break;
					case OrientationEnum.Down:
						faceDir = Orientation.Left;
						break;
					case OrientationEnum.Left:
						faceDir = Orientation.Up;
						break;
				}

				playerScript.playerDirectional.FaceDirection(faceDir);
				playerScript.playerDirectional.LockDirection = true;
			}

			if (playerScript.playerHealth.FireStacks <= 0)
			{
				playerScript.playerHealth.Extinguish();
			}
		}
		else if (playerScript.playerMove.IsCuffed) // Check if cuffed.
		{
			if (playerScript.playerSprites != null &&
				playerScript.playerSprites.clothes.TryGetValue("handcuffs", out var cuffsClothingItem))
			{
				if (cuffsClothingItem != null &&
					cuffsClothingItem.TryGetComponent<RestraintOverlay>(out var restraintOverlay))
				{
					restraintOverlay.ServerBeginUnCuffAttempt();
				}
			}
		}
	}

	/// <summary>
	/// Server handling of the request to drop an item from a client
	/// </summary>
	[Command]
	public void CmdDropItem(NamedSlot equipSlot)
	{
		//only allowed to drop from hands
		if (equipSlot != NamedSlot.leftHand && equipSlot != NamedSlot.rightHand) return;

		//allowed to drop from hands while cuffed
		if (!Validations.CanInteract(playerScript, NetworkSide.Server, allowCuffed: true)) return;
		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;

		var slot = itemStorage.GetNamedItemSlot(equipSlot);
		Inventory.ServerDrop(slot);
	}

	/// <summary>
	/// Request to drop alls item from ItemStorage, send an item slot net id of
	/// one of the slots on the item storage
	/// </summary>
	/// <param name="itemSlotID"></param>
	[Command]
	public void CmdDropAllItems(uint itemSlotID)
	{
		var netInstance = NetworkIdentity.spawned[itemSlotID];
		if (netInstance == null) return;

		var itemStorage = netInstance.GetComponent<ItemStorage>();
		if (this.itemStorage == null) return;

		var slots = itemStorage.GetItemSlots();
		if (slots == null) return;

		var validateSlot = itemStorage.GetIndexedItemSlot(0);
		if (validateSlot.RootPlayer() != playerScript.registerTile) return;

		foreach (var item in slots)
		{
			Inventory.ServerDrop(item);
		}
	}

	/// <summary>
	/// Completely disrobes another player
	/// </summary>
	[Command]
	public void CmdDisrobe(GameObject toDisrobe)
	{
		if (!Validations.CanApply(playerScript, toDisrobe, NetworkSide.Server)) return;
		//only allowed if this player is an observer of the player to disrobe
		var itemStorage = toDisrobe.GetComponent<ItemStorage>();
		if (itemStorage == null) return;

		//are we an observer of the player to disrobe?
		if (!itemStorage.ServerIsObserver(gameObject)) return;

		//disrobe each slot, taking .2s per each occupied slot
		//calculate time
		var occupiedSlots = itemStorage.GetItemSlots().Count(slot => slot.NamedSlot != NamedSlot.handcuffs && !slot.IsEmpty);
		if (occupiedSlots == 0) return;
		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;
		var timeTaken = occupiedSlots * .4f;
		void ProgressComplete()
		{ var victimsHealth = toDisrobe.GetComponent < PlayerHealth >();
			foreach (var itemSlot in itemStorage.GetItemSlots())
			{
				//skip slots which have special uses
				if (itemSlot.NamedSlot == NamedSlot.handcuffs) continue;
						// cancels out of the loop if player gets up
				if (!victimsHealth.IsCrit) break;

				Inventory.ServerDrop(itemSlot);
			}
		}

		StandardProgressAction.Create(DisrobeProgressConfig, ProgressComplete)
			.ServerStartProgress(toDisrobe.RegisterTile(), timeTaken, gameObject);
	}

	/// <summary>
	/// Server handling of the request to throw an item from a client
	/// </summary>
	[Command]
	public void CmdThrow(NamedSlot equipSlot, Vector3 worldTargetVector, int aim)
	{
		//only allowed to throw from hands
		if (equipSlot != NamedSlot.leftHand && equipSlot != NamedSlot.rightHand) return;
		if (!Validations.CanInteract(playerScript, NetworkSide.Server)) return;

		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;
		var slot = itemStorage.GetNamedItemSlot(equipSlot);
		Inventory.ServerThrow(slot, worldTargetVector,
			equipSlot == NamedSlot.leftHand ? SpinMode.Clockwise : SpinMode.CounterClockwise, (BodyPartType)aim);
	}

	[Command]
	public void CmdTryUncuff()
	{
		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;

		if (playerScript.playerSprites != null &&
			playerScript.playerSprites.clothes.TryGetValue("handcuffs", out var cuffsClothingItem))
		{
			if (cuffsClothingItem != null &&
				cuffsClothingItem.TryGetComponent<RestraintOverlay>(out var restraintOverlay))
			{
				restraintOverlay.ServerBeginUnCuffAttempt();
			}
		}
	}

	[Command]
	public void CmdInitiateRestartVote()
	{
		if (VotingManager.Instance == null) return;
		VotingManager.Instance.TryInitiateRestartVote(gameObject);
	}

	[Command]
	public void CmdRegisterVote(bool isFor)
	{
		if (VotingManager.Instance == null) return;
		var connectedPlayer = PlayerList.Instance.Get(gameObject);
		if (connectedPlayer == ConnectedPlayer.Invalid) return;
		VotingManager.Instance.RegisterVote(connectedPlayer.UserId, isFor);
	}

	[Command]
	public void CmdVetoRestartVote(string adminId, string adminToken)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;

		if (VotingManager.Instance == null) return;
		VotingManager.Instance.VetoVote(adminId);
	}

	/// <summary>
	/// Switches the pickup mode for the InteractableStorage in the players hands
	/// TODO should probably be turned into some kind of UIAction component which can hold all these functions
	/// </summary>
	[Command]
	public void CmdSwitchPickupMode()
	{
		// Switch the pickup mode of the storage in the active hand
		var storage = GetActiveHandItem()?.GetComponent<InteractableStorage>() ??
		              GetOffHandItem()?.GetComponent<InteractableStorage>();
		storage.ServerSwitchPickupMode(gameObject);
	}

	/// <summary>
	/// Everything that needs to be done when reentering body.
	/// </summary>
	/// <param name="recipient">The player to be synced.</param>
	[Server]
	public void ReenterBodyUpdates()
	{
		UpdateInventorySlots();
		TargetStopMusic(connectionToClient);
	}

	[TargetRpc]
	public void TargetStopMusic(NetworkConnection target)
	{
		MusicManager.SongTracker.Stop();
	}

	/// <summary>
	/// Make client a listener of each slot
	/// </summary>
	[Server]
	private void UpdateInventorySlots()
	{
		if (this == null || itemStorage == null || playerScript == null
			|| playerScript.mind == null || playerScript.mind.body == null)
		{
			return;
		}

		var body = playerScript.mind.body.gameObject;

		//player gets inventory slot updates again
		foreach (var slot in itemStorage.GetItemSlotTree())
		{
			slot.ServerAddObserverPlayer(body);
		}
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
				//Drop hand items when unconscious
				Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.leftHand));
				Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.rightHand));
				playerMove.allowInput = true;
				playerScript.PlayerSync.SpeedServer = playerMove.CrawlSpeed;
				if (oldState == ConsciousState.CONSCIOUS)
				{
					//only play the sound if we are falling
					SoundManager.PlayNetworkedAtPos("Bodyfall", transform.position, sourceObj: gameObject);
				}

				break;
			case ConsciousState.UNCONSCIOUS:
				//Drop items when unconscious
				Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.leftHand));
				Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.rightHand));
				playerMove.allowInput = false;
				if (oldState == ConsciousState.CONSCIOUS)
				{
					//only play the sound if we are falling
					SoundManager.PlayNetworkedAtPos("Bodyfall", transform.position, sourceObj: gameObject);
				}

				break;
		}

		playerScript.pushPull.ServerStopPulling();
	}

	[Server]
	public void CmdToggleChatIcon(bool turnOn, string message, ChatChannel chatChannel, ChatModifier chatModifier)
	{
		if (!playerScript.pushPull.VisibleState || (playerScript.mind.occupation.JobType == JobType.NULL)
												|| playerScript.playerHealth.IsDead || playerScript.playerHealth.IsCrit
												|| playerScript.playerHealth.IsCardiacArrest)
		{
			//Don't do anything with chat icon if player is invisible or not spawned in
			//This will also prevent clients from snooping other players local chat messages that aren't visible to them
			return;
		}

		// Cancel right away if the player cannot speak.
		if ((chatModifier & ChatModifier.Mute) == ChatModifier.Mute)
		{
			return;
		}

		ShowChatBubbleMessage.SendToNearby(gameObject, message, true, chatModifier);
	}

	[Command]
	public void CmdCommitSuicide()
	{
		GetComponent<LivingHealthBehaviour>().ApplyDamage(gameObject, 1000, AttackType.Internal, DamageType.Brute);
	}

	//Respawn action for Deathmatch v 0.1.3

	[Command]
	public void CmdRespawnPlayer()
	{
		if (GameManager.Instance.RespawnCurrentlyAllowed)
		{
			ServerRespawnPlayer();
		}
	}

	[Server]
	public void ServerRespawnPlayer(string occupation = null)
	{
		if (occupation != null)
		{
			foreach (var job in SOAdminJobsList.Instance.AdminAvailableJobs)
			{
				if (job.name != occupation)
				{
					continue;
				}

				playerScript.mind.occupation = job;
				break;
			}
		}

		StartCoroutine(CoRespawn());
	}

	[Server]
	IEnumerator CoRespawn()
	{
		if (playerScript.mind.occupation.JobType == JobType.SYNDICATE && !SubSceneManager.Instance.SyndicateLoaded)
		{
			//yield return StartCoroutine(SubSceneManager.Instance.LoadSyndicate());
		}

		PlayerSpawn.ServerRespawnPlayer(playerScript.mind);

		yield break;
	}

	[Command]
	public void CmdToggleAllowCloning()
	{
		playerScript.mind.DenyCloning = !playerScript.mind.DenyCloning;

		if (playerScript.mind.DenyCloning)
		{
			Chat.AddExamineMsgFromServer(gameObject, "<color=red>You will no longer be cloned</color>");
		}
		else
		{
			Chat.AddExamineMsgFromServer(gameObject, "<color=red>You can now be cloned</color>");
		}
	}

	/// <summary>
	/// Spawn the ghost for this player and tell the client to switch input / camera to it
	/// </summary>
	[Command]
	public void CmdSpawnPlayerGhost()
	{
		ServerSpawnPlayerGhost();
	}

	[Server]
	public void ServerSpawnPlayerGhost()
	{
		//Only force to ghost if the mind belongs in to that body
		var currentMobID = GetComponent<LivingHealthBehaviour>().mobID;
		if (GetComponent<LivingHealthBehaviour>().IsDead && !playerScript.IsGhost && playerScript.mind.bodyMobID == currentMobID)
		{
			PlayerSpawn.ServerSpawnGhost(playerScript.mind);
		}
	}

	/// <summary>
	/// Asks the server to let the client rejoin into a logged off character.
	/// </summary>
	///
	[Command]
	public void CmdGhostCheck()//specific check for if you want value returned
	{
		GhostEnterBody();
	}

	[Server]
	public void GhostEnterBody()
	{
		PlayerScript body = playerScript.mind.body;

		if (playerScript.mind.IsSpectator) return;

		if(playerScript.mind.ghostLocked) return;

		if (!playerScript.IsGhost )
		{
			Logger.LogWarningFormat("Either player {0} is not dead or not currently a ghost, ignoring EnterBody", Category.Health, body);
			return;
		}

		//body might be in a container, reentering should still be allowed in that case
		if (body.pushPull.parentContainer == null && body.WorldPos == TransformState.HiddenPos)
		{
			Logger.LogFormat("There's nothing left of {0}'s body, not entering it", Category.Health, body);
			return;
		}
		playerScript.mind.StopGhosting();
		PlayerSpawn.ServerGhostReenterBody(connectionToClient, gameObject, playerScript.mind);
		return;
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

	[Command]
	public void CmdSetActiveHand(NamedSlot hand)
	{
		activeHand = hand;
	}

	[Command]
	public void CmdPoint(GameObject pointTarget, Vector3 mousePos)
	{
		if (playerScript.IsGhost || playerScript.playerHealth.ConsciousState != ConsciousState.CONSCIOUS)
			return;
		string pointedName = pointTarget.name;
		var interactableTiles = pointTarget.GetComponent<InteractableTiles>();
		if (interactableTiles)
		{
			LayerTile tile = interactableTiles.LayerTileAt(mousePos);
			pointedName = tile.DisplayName;
		}
		Effect.PlayParticleDirectional(gameObject, mousePos);
		Chat.AddActionMsgToChat(playerScript.gameObject, $"You point at {pointedName}.", $"{playerScript.gameObject.name} points at {pointTarget.name}.");
	}

	[Command]
	public void CmdRequestPaperEdit(GameObject paper, string newMsg)
	{
		if (!Validations.CanInteract(playerScript, NetworkSide.Server)) return;

		//Validate paper edit request
		//TODO Check for Pen
		var leftHand = itemStorage.GetNamedItemSlot(NamedSlot.leftHand);
		var rightHand = itemStorage.GetNamedItemSlot(NamedSlot.rightHand);
		if (leftHand.Item?.gameObject == paper || rightHand.Item?.gameObject == paper)
		{
			var paperComponent = paper.GetComponent<Paper>();
			var pen = leftHand.Item?.GetComponent<Pen>();
			if (pen == null)
			{
				pen = rightHand.Item?.GetComponent<Pen>();
				if (pen == null)
				{
					//no pen
					paperComponent.UpdatePlayer(gameObject); //force server string to player
					return;
				}
			}

			if (paperComponent != null)
			{
				if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;
				paperComponent.SetServerString(newMsg);
				paperComponent.UpdatePlayer(gameObject);
			}
		}
	}

	/// <summary>
	/// A variation of CmdRequestPaperEdit, but is used for the PDA notes system
	/// </summary>
	[Command]
	public void CmdRequestNoteEdit(GameObject pdaObject, string newMsg)
	{
		if (!Validations.CanInteract(playerScript, NetworkSide.Server)) return;
		PDANotesNetworkHandler noteNetworkScript = pdaObject.GetComponent<PDANotesNetworkHandler>();
		noteNetworkScript.SetServerString(newMsg);
		noteNetworkScript.UpdatePlayer(gameObject);

	}

	[Command]
	public void CmdRequestRename(GameObject target, string customName)
	{
		var rename = target.GetComponent<Renameable>();

		if (rename == null)
		{
			return;
		}

		if (customName.Length > 42)
		{
			customName = customName.Substring(0, 42);
		}

		customName = Regex.Replace(customName, "<size=\"(.*)\">", "", RegexOptions.IgnoreCase);
		customName = customName.Replace("</size>", "");

		rename.SetCustomName(customName);
	}

	[Command]
	public void CmdRequestItemLabel(GameObject handLabeler, string label)
	{
		ItemStorage itemStorage = gameObject.GetComponent<ItemStorage>();
		Pickupable handItem = itemStorage.GetActiveHandSlot().Item;
		if (handItem == null) return;
		if (handItem.gameObject != handLabeler) return;

		Chat.AddExamineMsgFromServer(gameObject, "You set the " + handLabeler.Item().InitialName.ToLower() + "s text to '" + label + "'.");
		handLabeler.GetComponent<HandLabeler>().SetLabel(label);
	}

	[Command]
	public void CmdGhostPerformTeleport(Vector3 s3)
	{
		ServerGhostPerformTeleport(s3);
	}

	[Server]
	public void ServerGhostPerformTeleport(Vector3 s3)
	{
		if (playerScript.IsGhost && Math.Abs(s3.x) <= 20000 && Math.Abs(s3.y) <= 20000)
		{
			playerScript.PlayerSync.SetPosition(s3); //server forces position on player
		}
	}

	//admin only commands
	#region Admin

	[Command]
	public void CmdAGhost(string adminId, string adminToken)
	{
		ServerAGhost(adminId, adminToken);
	}

	[Server]
	public void ServerAGhost(string adminId, string adminToken)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;

		if (!playerScript.IsGhost)//admin turns into ghost
		{
			PlayerSpawn.ServerSpawnGhost(playerScript.mind);
		}
		else if (playerScript.IsGhost)//back to player
		{
			if (playerScript.mind.IsSpectator) return;

			GhostEnterBody();
		}
	}

	[Command]
	public void CmdAdminMakeHotspot(GameObject onObject, string adminId, string adminToken)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;
		if (onObject == null) return;
		var reactionManager = onObject.GetComponentInParent<ReactionManager>();
		if (reactionManager == null) return;

		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition(), 700, .5f);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.down, 700, .05f);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.left, 700, .05f);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.up, 700, .05f);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.right, 700, .05f);
	}

	[Command]
	public void CmdAdminSmash(GameObject toSmash, string adminId, string adminToken)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;

		if (toSmash == null)
		{
			return;
		}

		var integrity = toSmash.GetComponent<Integrity>();
		if (integrity == null)
		{
			return;
		}
		integrity.ApplyDamage(float.MaxValue, AttackType.Melee, DamageType.Brute);
	}

	[Command]
	public void CmdGetAdminOverlayFullUpdate(string adminId, string adminToken)
	{
		AdminOverlay.RequestFullUpdate(adminId, adminToken);
	}

	#endregion

	[Command]
	public void CmdRequestSpell(int spellIndex)
	{
		foreach (var spell in playerScript.mind.Spells)
		{
			if (spell.SpellData.Index == spellIndex)
			{
				spell.CallActionServer(PlayerList.Instance.Get(gameObject));
				return;
			}
		}
	}
}
