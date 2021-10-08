using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AdminTools;
using Items.PDA;
using UnityEngine;
using Mirror;
using Audio.Containers;
using ScriptableObjects;
using AdminCommands;
using Antagonists;
using Systems.Atmospherics;
using HealthV2;
using Items;
using Items.Tool;
using Messages.Server;
using Player.Movement;
using Shuttles;
using UI.Core;
using UI.Items;


public partial class PlayerNetworkActions : NetworkBehaviour
{
	private static readonly StandardProgressActionConfig DisrobeProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Disrobe);

	// For access checking. Must be nonserialized.
	// This has to be added because using the UIManager at client gets the server's UIManager. So instead I just had it send the active hand to be cached at server.
	[NonSerialized] public GameObject activeHand;
	[NonSerialized] public NamedSlot CurrentActiveHand = NamedSlot.rightHand;
	//synchronise uint of arm for hand slot


	private Equipment equipment = null;

	private PlayerMove playerMove;
	private PlayerScript playerScript;
	public DynamicItemStorage itemStorage => playerScript.DynamicItemStorage;
	public Transform chatBubbleTarget;

	public bool IsRolling { get; private set; } = false;

	private void Awake()
	{
		playerMove = GetComponent<PlayerMove>();
		playerScript = GetComponent<PlayerScript>();
	}

	/// <summary>
	/// Get the item in the player's active hand
	/// </summary>
	/// <returns>the gameobject item in the player's active hand, null if nothing in active hand</returns>
	public GameObject GetActiveHandItem()
	{
		var pu = itemStorage.GetActiveHandSlot().ItemObject;
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
		NamedSlot enumA = (NamedSlot) Enum.Parse(typeof(NamedSlot), slotName);
		equipment.SetReference((int) enumA, Item);
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
		else if (playerScript.playerHealth.FireStacks > 0
		) // Check if we are on fire. If we are perform a stop-drop-roll animation and reduce the fire stacks.
		{
			Chat.AddActionMsgToChat(
				playerScript.gameObject,
				"You drop to the ground and frantically try to put yourself out!",
				$"{playerScript.playerName} is trying to extinguish themself!");
			StartCoroutine(Roll());
		}
		else if (playerScript.playerMove.IsCuffed) // Check if cuffed.
		{
			if (playerScript.playerSprites != null &&
			    playerScript.playerSprites.clothes.TryGetValue(NamedSlot.handcuffs, out var cuffsClothingItem))
			{
				if (cuffsClothingItem != null &&
				    cuffsClothingItem.TryGetComponent<RestraintOverlay>(out var restraintOverlay))
				{
					restraintOverlay.ServerBeginUnCuffAttempt();
				}
			}
		}
		else if (playerScript.playerMove.IsTrapped) // Check if trapped.
		{
			playerScript.PlayerSync.ServerTryEscapeContainer();
		}
	}

	/// <summary>
	/// Handles the verification and execution of the stop, drop, and roll process
	/// </summary>
	IEnumerator Roll()
	{
		//Can't roll if you're already rolling or have slipped
		if (IsRolling || playerScript.registerTile.IsSlippingServer)
		{
			yield return null;
		}

		IsRolling = true;

		// Drop the player if they aren't already, prevent them from moving until the action is complete
		if (playerScript.registerTile.IsLayingDown == false)
		{
			playerScript.registerTile.ServerSetIsStanding(false);
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Bodyfall, transform.position, sourceObj: gameObject);
		}
		playerScript.playerMove.allowInput = false;

		// Drop player items

		foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.leftHand))
		{
			Inventory.ServerDrop(itemSlot);
		}

		foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.rightHand))
		{
			Inventory.ServerDrop(itemSlot);
		}


		//Remove fire and do part of a roll every .2 seconds
		while (playerScript.playerHealth.FireStacks > 0)
		{
			//Can only roll if you're conscious and not stunned
			if (playerScript.playerHealth.ConsciousState != ConsciousState.CONSCIOUS ||
				playerScript.registerTile.IsSlippingServer)
			{
				break;
			}

			// Remove 1/2 a stack per roll action.
			playerScript.playerHealth.ChangeFireStacks(-0.5f);

			// Find the next in the roll sequence. Also unlock the facing direction temporarily since laying down locks it.
			playerScript.playerDirectional.LockDirection = false;
			playerScript.playerDirectional.FaceDirection(playerScript.playerDirectional.CurrentDirection.Rotate(RotationOffset.Right));
			playerScript.playerDirectional.LockDirection = true;

			yield return WaitFor.Seconds(0.2f);
		}

		//If rolling is interrupted with a stun or unconsciousness, don't finalise the action
		if (playerScript.playerHealth.FireStacks == 0)
		{
			playerScript.playerHealth.Extinguish();
			playerScript.registerTile.ServerStandUp(true);
			playerScript.playerMove.allowInput = true;
		}

		//Allow barely conscious players to move again if they are not stunned
		if (playerScript.playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS
			&& playerScript.registerTile.IsSlippingServer == false) {
			playerScript.playerMove.allowInput = true;
		}

		IsRolling = false;
		yield return null;
	}

	[Command]
	public void CmdSlideItem(Vector3Int destination)
	{
		if (playerScript.IsPositionReachable(destination, true) == false
			|| playerScript.pushPull.PulledObjectServer == null
			|| playerScript.IsGhost
			|| playerScript.playerHealth.ConsciousState != ConsciousState.CONSCIOUS)
		{
			return;
		}
		PushPull pushPull = playerScript.pushPull.PulledObjectServer;
		Vector3Int origin = pushPull.registerTile.WorldPositionServer;
		Vector2Int dir = (Vector2Int)(destination - origin);
		pushPull.TryPush(dir);
	}

	/// <summary>
	/// Server handling of the request to drop an item from a client
	/// </summary>
	[Command]
	public void CmdDropItem(uint NetID, NamedSlot equipSlot)
	{
		//only allowed to drop from hands
		if (equipSlot != NamedSlot.leftHand && equipSlot != NamedSlot.rightHand) return;

		//allowed to drop from hands while cuffed
		if (!Validations.CanInteract(playerScript, NetworkSide.Server, allowCuffed: true)) return;
		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;
		if (NetworkIdentity.spawned.ContainsKey(NetID) == false) return;
		var Object = NetworkIdentity.spawned[NetID].gameObject;



		var slot = itemStorage.GetNamedItemSlot(Object,equipSlot);
		if (slot == null) return;
		Inventory.ServerDrop(slot);
	}

	/// <summary>
	/// Request to drop alls item from ItemStorage, send an item slot net id of
	/// one of the slots on the item storage
	/// </summary>
	/// <param name="itemSlotID"></param>
	[Command]
	public void CmdDropAllItems(uint itemSlotID, Vector3 Target)
	{
		var netInstance = NetworkIdentity.spawned[itemSlotID];
		if (netInstance == null) return;

		var itemStorage = netInstance.GetComponent<ItemStorage>();
		if (this.itemStorage == null) return;

		var slots = itemStorage.GetItemSlots();
		if (slots == null) return;

		var validateSlot = itemStorage.GetIndexedItemSlot(0);
		if (validateSlot.RootPlayer() != playerScript.registerTile) return;


		Vector2? possibleTarget = null;
		if (Target != TransformState.HiddenPos)
		{
			if (Validations.IsReachableByPositions(PlayerManager.PlayerScript.registerTile.WorldPosition, Target, false))
			{
				if (MatrixManager.IsPassableAtAllMatricesOneTile(Target.RoundToInt(), CustomNetworkManager.Instance._isServer))
				{
					possibleTarget = (Target - PlayerManager.PlayerScript.registerTile.WorldPosition);
				}
			}
		}

		foreach (var item in slots)
		{
			Inventory.ServerDrop(item, possibleTarget);
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
		var dynamicItemStorage = toDisrobe.GetComponent<DynamicItemStorage>();
		if (dynamicItemStorage == null) return;

		//disrobe each slot, taking .2s per each occupied slot
		//calculate time
		var occupiedSlots = dynamicItemStorage.GetItemSlots()
			.Count(slot => slot.NamedSlot != NamedSlot.handcuffs && !slot.IsEmpty);

		if (occupiedSlots == 0) return;

		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;

		var timeTaken = occupiedSlots * .4f;
		void ProgressComplete()
		{
			var victimsHealth = toDisrobe.GetComponent<PlayerHealthV2>();
			foreach (var itemSlot in dynamicItemStorage.GetItemSlots())
			{
				//are we an observer of the player to disrobe?
				if (itemSlot.ServerIsObservedBy(gameObject) == false) continue;

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
	public void CmdThrow( Vector3 worldTargetVector, int aim)
	{
		//only allowed to throw from hands
		if (!Validations.CanInteract(playerScript, NetworkSide.Server)) return;

		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;
		var slot = itemStorage.GetActiveHandSlot();
		Inventory.ServerThrow(slot, worldTargetVector,
			slot.NamedSlot == NamedSlot.leftHand ? SpinMode.Clockwise : SpinMode.CounterClockwise, (BodyPartType) aim);
	}

	[Command]
	public void CmdTryUncuff()
	{
		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;

		if (playerScript.playerSprites != null &&
		    playerScript.playerSprites.clothes.TryGetValue(NamedSlot.handcuffs, out var cuffsClothingItem))
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
		VotingManager.Instance.TryInitiateRestartVote(gameObject, connectionToClient);
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
	public void CmdVetoRestartVote()
	{
		if (AdminCommandsManager.IsAdmin(connectionToClient, out var player))
		{
			if (VotingManager.Instance == null) return;

			VotingManager.Instance.VetoVote(player.ClientId);
		}
	}

	/// <summary>
	/// Switches the pickup mode for the InteractableStorage in the players hands
	/// TODO should probably be turned into some kind of UIAction component which can hold all these functions
	/// </summary>
	[Command]
	public void CmdSwitchPickupMode()
	{
		// Switch the pickup mode of the storage in the active hand
		InteractableStorage storage = null;
		foreach (var itemSlot in itemStorage.GetNamedItemSlots(NamedSlot.rightHand))
		{
			if (itemSlot.ItemObject != null && itemSlot.ItemObject.TryGetComponent<InteractableStorage>(out storage))
			{
				break;
			}
		}

		if (storage == null)
		{
			foreach (var itemSlot in itemStorage.GetNamedItemSlots(NamedSlot.leftHand))
			{
				if (itemSlot.ItemObject != null && itemSlot.ItemObject.TryGetComponent<InteractableStorage>(out storage))
				{
					break;
				}
			}
		}

		if (storage != null)
		{
			storage.ServerSwitchPickupMode(gameObject);
		}
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
				foreach (var itemSlot in itemStorage.GetHandSlots())
				{
					Inventory.ServerDrop(itemSlot);
				}
				playerMove.allowInput = true;
				playerScript.PlayerSync.SpeedServer = playerMove.CrawlSpeed;
				if (oldState == ConsciousState.CONSCIOUS)
				{
					//only play the sound if we are falling
					SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Bodyfall, transform.position, sourceObj: gameObject);
				}

				break;
			case ConsciousState.UNCONSCIOUS:
				//Drop items when unconscious
				foreach (var itemSlot in itemStorage.GetHandSlots())
				{
					Inventory.ServerDrop(itemSlot);
				}
				playerMove.allowInput = false;
				if (oldState == ConsciousState.CONSCIOUS)
				{
					//only play the sound if we are falling
					SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Bodyfall, transform.position, sourceObj: gameObject);
				}

				break;
		}

		playerScript.pushPull.ServerStopPulling();
	}

	[Server]
	public void ServerToggleChatIcon(bool turnOn, string message, ChatChannel chatChannel, ChatModifier chatModifier)
	{
		if (!playerScript.pushPull.VisibleState || (playerScript.mind.occupation.JobType == JobType.NULL
		                                        || playerScript.playerHealth.IsDead || playerScript.playerHealth.IsCrit))
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
		GetComponent<LivingHealthMasterBase>().ApplyDamageAll(gameObject, 1000, AttackType.Internal, DamageType.Brute);
	}

	// Respawn action for Deathmatch v 0.1.3

	[Command]
	public void CmdRespawnPlayer()
	{
		if (AdminCommandsManager.IsAdmin(connectionToClient, out _, false) || GameManager.Instance.RespawnCurrentlyAllowed)
		{
			ServerRespawnPlayer();
		}
	}

	[Server]
	public void ServerRespawnPlayer(string occupation = null)
	{
		if (occupation != null)
		{
			foreach (var job in OccupationList.Instance.Occupations)
			{
				if (job.name != occupation)
				{
					continue;
				}

				playerScript.mind.occupation = job;
				break;
			}
		}

		//Can be null if respawning spectator ghost as they dont have an occupation
		if (playerScript.mind.occupation == null)
		{
			return;
		}

		PlayerSpawn.ServerRespawnPlayer(playerScript.mind);
	}

	[Server]
	public void ServerRespawnPlayerSpecial(string occupation = null)
	{
		if (occupation != null)
		{
			foreach (var job in SOAdminJobsList.Instance.SpecialJobs)
			{
				if (job.name != occupation)
				{
					continue;
				}

				playerScript.mind.occupation = job;
				break;
			}
		}

		PlayerSpawn.ServerRespawnPlayer(playerScript.mind);
	}

	[Server]
	public void ServerRespawnPlayerAntag(ConnectedPlayer playerToRespawn, string antagonist)
	{
		foreach (var antag in SOAdminJobsList.Instance.Antags)
		{
			if (antag.AntagName != antagonist)
			{
				continue;
			}

			StartCoroutine(AntagManager.Instance.ServerRespawnAsAntag(playerToRespawn, antag));
			return;
		}

		Logger.LogWarning($"Antagonist string \"{antagonist}\" not found in {nameof(SOAdminJobsList)}!", Category.Antags);
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
		var currentMobID = GetComponent<LivingHealthMasterBase>().mobID;
		if (GetComponent<LivingHealthMasterBase>().IsDead && !playerScript.IsGhost && playerScript.mind != null &&
		    playerScript.mind.bodyMobID == currentMobID)
		{
			PlayerSpawn.ServerSpawnGhost(playerScript.mind);
		}
	}

	/// <summary>
	/// Asks the server to let the client rejoin into a logged off character.
	/// </summary>
	///
	[Command]
	public void CmdGhostCheck() // specific check for if you want value returned
	{
		GhostEnterBody();
	}

	[Server]
	public void GhostEnterBody()
	{
		PlayerScript body = playerScript.mind.body;

		if (playerScript.mind.IsSpectator) return;

		if (playerScript.mind.ghostLocked) return;

		if (!playerScript.IsGhost)
		{
			Logger.LogWarningFormat("Either player {0} is not dead or not currently a ghost, ignoring EnterBody",
				Category.Ghosts, body);
			return;
		}

		//body might be in a container, reentering should still be allowed in that case
		if (body.pushPull != null && body.pushPull.parentContainer == null && body.WorldPos == TransformState.HiddenPos)
		{
			Logger.LogFormat("There's nothing left of {0}'s body, not entering it", Category.Ghosts, body);
			return;
		}

		playerScript.mind.StopGhosting();
		PlayerSpawn.ServerGhostReenterBody(connectionToClient, gameObject, playerScript.mind);
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
	public void CmdSetActiveHand(uint handID, NamedSlot NamedSlot)
	{
		if (handID != 0 && NetworkIdentity.spawned.ContainsKey(handID) == false) return;
		if (NamedSlot != NamedSlot.leftHand && NamedSlot != NamedSlot.rightHand && NamedSlot != NamedSlot.none) return;
		if (playerScript.IsGhost) return; // Because Ghosts don't have dynamic item storage

		if (handID != 0)
		{
			var slot = playerScript.DynamicItemStorage.GetNamedItemSlot(NetworkIdentity.spawned[handID].gameObject, NamedSlot);
			if (slot == null) return;
			activeHand = NetworkIdentity.spawned[handID].gameObject;
		}
		else
		{
			activeHand = null;
		}
		CurrentActiveHand = NamedSlot;

	}

	[Command]
	public void CmdPoint(GameObject pointTarget, Vector3 mousePos)
	{
		if (Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction) == false)
			return;
		if (playerScript.IsGhost || playerScript.playerHealth.ConsciousState != ConsciousState.CONSCIOUS)
			return;

		//If we are trying to find matrix get matrix instead
		if (pointTarget.TryGetComponent<MatrixSync>(out var matrixSync))
		{
			pointTarget = matrixSync.NetworkedMatrix.gameObject;
		}

		string pointedName = pointTarget.ExpensiveName();
		var interactableTiles = pointTarget.GetComponent<InteractableTiles>();
		if (interactableTiles)
		{
			LayerTile tile = interactableTiles.LayerTileAt(mousePos);
			if (tile != null) // null if space
			{
				pointedName = tile.DisplayName;
			}
		}

		var livinghealthbehavior = pointTarget.GetComponent<LivingHealthMasterBase>();
		var preposition = "";
		if (livinghealthbehavior == null)
			preposition = "the ";

		Effect.PlayParticleDirectional(gameObject, mousePos);
		Chat.AddActionMsgToChat(playerScript.gameObject, $"You point at {preposition}{pointedName}.",
			$"{playerScript.gameObject.ExpensiveName()} points at {preposition}{pointedName}.");
	}

	[Command]
	public void CmdRequestPaperEdit(GameObject paper, string newMsg)
	{
		if (!Validations.CanInteract(playerScript, NetworkSide.Server)) return;

		//Validate paper edit request
		//TODO Check for Pen
		foreach (var itemSlot in  itemStorage.GetHandSlots())
		{
			if (itemSlot.ItemObject == paper)
			{
				var paperComponent = paper.GetComponent<Paper>();
				Pen pen = null;
				foreach (var PenitemSlot in itemStorage.GetHandSlots())
				{
					pen = PenitemSlot.ItemObject?.GetComponent<Pen>();
					if (pen != null)
					{
						break;
					}
				}

				if (pen == null)
				{
					//no pen
					paperComponent.UpdatePlayer(gameObject); //force server string to player
					return;
				}
				if (paperComponent != null)
				{
					if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;
					paperComponent.SetServerString(newMsg);
					paperComponent.UpdatePlayer(gameObject);
				}
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
		DynamicItemStorage itemStorage = gameObject.GetComponent<DynamicItemStorage>();
		Pickupable handItem = itemStorage.GetActiveHandSlot()?.Item;
		if (handItem == null) return;
		if (handItem.gameObject != handLabeler) return;

		Chat.AddExamineMsgFromServer(gameObject,
			"You set the " + handLabeler.Item().InitialName.ToLower() + "s text to '" + label + "'.");
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

	#region Admin-only

	[Command]
	public void CmdAGhost()
	{
		if (AdminCommandsManager.IsAdmin(connectionToClient, out _))
		{
			ServerAGhost();
		}
	}

	[Server]
	public void ServerAGhost()
	{
		if (!playerScript.IsGhost || playerScript.IsPlayerSemiGhost)//admin turns into ghost
		{
			PlayerSpawn.ServerSpawnGhost(playerScript.mind);
		}
		else if (playerScript.IsGhost) //back to player
		{
			if (playerScript.mind.IsSpectator) return;

			GhostEnterBody();
		}
	}

	[Command]
	public void CmdAdminMakeHotspot(GameObject onObject)
	{
		if (AdminCommandsManager.IsAdmin(connectionToClient, out _) == false) return;

		if (onObject == null) return;
		var reactionManager = onObject.GetComponentInParent<ReactionManager>();
		if (reactionManager == null) return;

		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition(), 1000, true);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.down, 1000, true);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.left, 1000, true);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.up, 1000, true);
		reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.right, 1000, true);
	}

	[Command]
	public void CmdAdminSmash(GameObject toSmash)
	{
		if (AdminCommandsManager.IsAdmin(connectionToClient, out _) == false) return;

		if (toSmash == null) return;

		var integrity = toSmash.GetComponent<Integrity>();
		if (integrity == null) return;

		integrity.ApplyDamage(float.MaxValue, AttackType.Melee, DamageType.Brute);
	}

	[Command]
	public void CmdGetAdminOverlayFullUpdate()
	{
		if (AdminCommandsManager.IsAdmin(connectionToClient, out var player))
		{
			AdminOverlay.RequestFullUpdate(player);
		}
	}

	#endregion

	// If we end up needing more information to send to server,
	// probably best to create a new interaction type and use IF2.
	[Command]
	public void CmdRequestSpell(int spellIndex, Vector3 clickPosition)
	{
		foreach (var spell in playerScript.mind.Spells)
		{
			if (spell.SpellData.Index == spellIndex)
			{
				spell.CallActionServer(PlayerList.Instance.Get(gameObject), clickPosition);
				return;
			}
		}
	}

	[Command]
	public void CmdSetCrayon(GameObject crayon, uint category, uint index, uint colourIndex, OrientationEnum direction)
	{
		if(crayon == null || crayon.TryGetComponent<CrayonSprayCan>(out var crayonScript) ==  false) return;

		crayonScript.SetTileFromClient(category, index, colourIndex, direction);
	}

	[Command]
	public void CmdAskforAntagObjectives()
	{
		playerScript.mind.ShowObjectives();
	}

	[TargetRpc]
	public void TargetRpcOpenInput(GameObject objectForInput, string title, string currentText)
	{
		if(objectForInput == null) return;

		UIManager.Instance.GeneralInputField.OnOpen(objectForInput, title, currentText);
	}

	[Command]
	public void CmdFilledDynamicInput(GameObject forGameObject, string input)
	{
		if(forGameObject == null) return;

		foreach (var dynamicInput in forGameObject.GetComponents<IDynamicInput>())
		{
			dynamicInput.OnInputFilled(input, playerScript);
		}
	}
}
