using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Items.PDA;
using UnityEngine;
using Mirror;
using Audio.Containers;
using ScriptableObjects;
using AdminCommands;
using Antagonists;
using Blob;
using Core.Chat;
using HealthV2;
using Items;
using Items.Tool;
using Messages.Server;
using Objects.Other;
using Shuttles;
using UI.Core;
using UI.Items;
using Doors;
using Logs;
using Managers;
using Objects;
using Player.Language;
using Systems.Ai;
using Systems.Faith;
using Tiles;
using Util;
using Random = UnityEngine.Random;

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

	private MovementSynchronisation playerMove;
	private PlayerScript playerScript;
	public DynamicItemStorage itemStorage => playerScript.DynamicItemStorage;
	public Transform chatBubbleTarget;

	public bool IsRolling { get; private set; } = false;

	private void Awake()
	{
		playerMove = GetComponent<MovementSynchronisation>();
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

	[Command]
	public void CmdSetCurrentIntent(Intent intent)
	{
		if (playerScript.OrNull()?.playerMove == null)
		{
			Loggy.LogError($"null playerScript/playerMove in {this.name} ");
			return;
		}
		playerScript.playerMove.intent = intent;
		playerScript.OnIntentChange?.Invoke(intent);
	}


	/// <summary>
	/// Server handling of the request to perform a resist action.
	/// </summary>
	[Command]
	public void CmdResist()
	{
		if (Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction) == false) return;

		if(playerScript.PlayerTypeSettings.CanResist == false) return;

		// Handle the movement restricted actions first.
		if (playerScript.playerMove.BuckledToObject != null)
		{
			//If we are buckled we need to unbuckle first before removing hand cuffs
			playerScript.playerMove.BuckledToObject.GetComponent<BuckleInteract>().TryUnbuckle(playerScript);
			return;
		}
		// Check if we are on fire. If we are perform a stop-drop-roll animation and reduce the fire stacks.
		if (playerScript.playerHealth.FireStacks > 0)
		{
			Chat.AddActionMsgToChat(
				playerScript.gameObject,
				"You drop to the ground and frantically try to put yourself out!",
				$"{playerScript.playerName} is trying to extinguish themself!");
			StartCoroutine(Roll());
			return;
		}

		// Check if cuffed.
		if (playerScript.playerMove.IsCuffed)
		{
			if (playerScript.playerSprites != null &&
			    playerScript.playerSprites.clothes.TryGetValue(NamedSlot.handcuffs, out var cuffsClothingItem))
			{
				if (cuffsClothingItem != null && cuffsClothingItem.TryGetComponent<RestraintOverlay>(out var restraintOverlay))
				{
					restraintOverlay.ServerBeginUnCuffAttempt();
				}
			}
			return;
		}

		// Check if trapped.
		if (playerScript.playerMove.BuckledToObject != null || playerScript.playerMove.ContainedInObjectContainer != null)
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
		if (IsRolling || playerScript.RegisterPlayer.IsSlippingServer)
		{
			yield return null;
		}

		IsRolling = true;

		// Drop the player if they aren't already, prevent them from moving until the action is complete
		if (playerScript.RegisterPlayer.IsLayingDown == false)
		{
			playerScript.RegisterPlayer.ServerSetIsStanding(false);
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Bodyfall, transform.position, sourceObj: gameObject);
		}
		playerScript.playerMove.ServerAllowInput.RecordPosition(this, false);

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
				playerScript.RegisterPlayer.IsSlippingServer)
			{
				break;
			}

			// Remove 1/2 a stack per roll action.
			playerScript.playerHealth.ChangeFireStacks(-0.5f);

			// Find the next in the roll sequence. Also unlock the facing direction temporarily since laying down locks it.
			playerScript.PlayerDirectional.LockDirectionTo(false, playerScript.PlayerDirectional.CurrentDirection);
			playerScript.PlayerDirectional.RotateBy(2);
			playerScript.PlayerDirectional.LockDirectionTo(true, playerScript.PlayerDirectional.CurrentDirection);

			yield return WaitFor.Seconds(0.2f);
		}

		//If rolling is interrupted with a stun or unconsciousness, don't finalise the action
		if (playerScript.playerHealth.FireStacks == 0)
		{
			playerScript.playerHealth.Extinguish();
			playerScript.RegisterPlayer.ServerStandUp(true);
			playerScript.playerMove.ServerAllowInput.RemovePosition(this);
		}

		//Allow barely conscious players to move again if they are not stunned
		if (playerScript.playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS
			&& playerScript.RegisterPlayer.IsSlippingServer == false) {
			playerScript.playerMove.ServerAllowInput.RemovePosition(this);
		}

		IsRolling = false;
		yield return null;
	}

	[Command]
	public void CmdSlideItem(Vector3Int destination)
	{
		if (playerScript.ObjectPhysics.Pulling.HasComponent == false) return;
		if (playerScript.IsGhost) return;
		if (playerScript.playerHealth.ConsciousState != ConsciousState.CONSCIOUS) return;
		if (playerScript.IsPositionReachable(destination, true) == false) return;

		var pushPull = playerScript.ObjectPhysics.Pulling.Component;
		Vector3Int origin = pushPull.registerTile.WorldPositionServer;
		Vector2Int dir = (Vector2Int)(destination - origin);
		pushPull.TryTilePush(dir, null, speed: playerScript.ObjectPhysics.CurrentTileMoveSpeed, overridePull: true);
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
		if (Validations.CanInteract(playerScript, NetworkSide.Server, allowCuffed: true,
			    apt: Validations.CheckState(x => x.CanDropItems)) == false) return;

		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;
		if (NetworkServer.spawned.TryGetValue(NetID, out var objectToDrop) == false) return;

		var slot = itemStorage.GetNamedItemSlot(objectToDrop.gameObject, equipSlot);
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
		var netInstance = NetworkServer.spawned[itemSlotID];
		if (netInstance == null) return;

		var storage = netInstance.GetComponent<ItemStorage>();
		if (this.itemStorage == null) return;

		var slots = storage.GetItemSlots();
		if (slots == null) return;

		var validateSlot = storage.GetIndexedItemSlot(0);
		if (validateSlot.RootPlayer() != playerScript.RegisterPlayer) return;


		Vector2? possibleTarget = null;
		if (Target != TransformState.HiddenPos)
		{
			if (Validations.IsReachableByPositions(PlayerManager.LocalPlayerScript.RegisterPlayer.WorldPosition, Target, false))
			{
				if (MatrixManager.IsPassableAtAllMatricesOneTile(Target.RoundToInt(), CustomNetworkManager.Instance._isServer))
				{
					possibleTarget = (Target - PlayerManager.LocalPlayerScript.RegisterPlayer.WorldPosition);
				}
			}
		}

		foreach (var item in slots)
		{
			Inventory.ServerDrop(item, possibleTarget);
		}
	}

	/// <summary>
	/// Transfers x amount of items from one hand to another. For stackable items only
	/// </summary>
	[Command]
	public void CmdSplitStack(uint fromSlotID, NamedSlot fromSlot, int amountToTransfer)
	{
		if (fromSlot != NamedSlot.leftHand && fromSlot != NamedSlot.rightHand) return; //Only allowed to transfer from one hand to another
		if (!Validations.CanInteract(playerScript, NetworkSide.Server, allowCuffed: false)) return; //Not allowed to transfer while cuffed
		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;

		ItemSlot emptySlot = playerScript.DynamicItemStorage.GetActiveHandSlot(); //Were assuming that slot to which player wants to transfer stuff is always active hand

		if(NetworkServer.spawned.TryGetValue(fromSlotID, out var objFS) == false) return;
		var stackSlot = itemStorage.GetNamedItemSlot(objFS.gameObject, fromSlot);

		if (stackSlot.ServerIsObservedBy(gameObject) == false || emptySlot.ServerIsObservedBy(gameObject) == false) return; //Checking if we can observe our hands

		if (stackSlot.ItemObject == null || emptySlot.ItemObject != null) return;
		if (stackSlot.ItemObject.TryGetComponent<Stackable>(out var stackSlotStackable) == false) return;
		if (stackSlotStackable.Amount < amountToTransfer || amountToTransfer <= 0) return;

		var multiple = Spawn.ServerPrefab(Spawn.DeterminePrefab(stackSlot.ItemObject)).GameObject;
		multiple.GetComponent<Stackable>().ServerSetAmount(amountToTransfer);
		Inventory.ServerAdd(multiple, emptySlot);
		stackSlotStackable.ServerConsume(amountToTransfer);
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
	public void CmdThrow(Vector3 targetLocalPosition, int aim, Vector3 clientWorldOfDifference) //TODO Should probably check distance to pull the object
	{
		//only allowed to throw from hands
		if (Validations.CanInteract(playerScript, NetworkSide.Server,
			    apt: Validations.CheckState(x => x.CanThrowItems)) == false) return;

		if (Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction) == false) return;

		var slot = itemStorage.GetActiveHandSlot();
		Vector3 targetVector = targetLocalPosition.ToWorld(playerMove.registerTile.Matrix) - playerMove.transform.position;
		if (slot?.Item != null)
		{
			Inventory.ServerThrow(slot, targetVector, (BodyPartType) aim);
		}
		else if (playerMove.Pulling.HasComponent)
		{

			if ((playerMove.Pulling.Component as MovementSynchronisation) == null)
			{
				if (playerMove.Pulling.Component.attributes.Component.OrNull()?.Size != null && playerMove.Pulling.Component.attributes.Component.Size >= Size.Large)
				{
					return;
				}
			}
			targetVector = targetLocalPosition.ToWorld(playerMove.registerTile.Matrix) - playerMove.Pulling.Component.gameObject.AssumedWorldPosServer();
			var distance = targetVector.magnitude;

			if (distance > 6)
			{
				distance = 6;
			}

			if ((playerMove.transform.position - playerMove.Pulling.Component.gameObject.AssumedWorldPosServer()).magnitude >
			    PlayerScript.INTERACTION_DISTANCE_EXTENDED) //If telekinesis was used play effect
			{
				PlayEffect.SendToAll(playerMove.Pulling.Component.gameObject, "TelekinesisEffect");
			}

			var pulling = playerMove.Pulling.Component;

			playerMove.StopPulling(false);

			//TODO speedloss  / friction
			var speed = 8;

			pulling.NewtonianPush( targetVector,speed,
				(distance / speed ) - ((Mathf.Pow(speed, 2) / (2*UniversalObjectPhysics.DEFAULT_Friction)) / speed),
				Single.NaN, (BodyPartType) aim, this.gameObject, Random.Range(25, 150));
		}



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
	public void CmdInitiateGameModeVote()
	{
		if (VotingManager.Instance == null) return;
		VotingManager.Instance.TryInitiateNextGameModeVote(gameObject, connectionToClient);
	}

	[Command]
	public void CmdInitiateMapVote()
	{
		if (VotingManager.Instance == null) return;
		VotingManager.Instance.TryInitiateNextMapVote(gameObject, connectionToClient);
	}

	[Command]
	public void CmdRegisterVote(string isFor)
	{
		if (VotingManager.Instance == null) return;
		var connectedPlayer = PlayerList.Instance.GetOnline(gameObject);
		if (connectedPlayer == PlayerInfo.Invalid) return;
		VotingManager.Instance.RegisterVote(connectedPlayer.UserId, isFor);
	}

	[Command]
	public void CmdVetoRestartVote()
	{
		if (AdminCommandsManager.IsAdmin(connectionToClient, out var player))
		{
			if (VotingManager.Instance == null) return;

			VotingManager.Instance.VetoVote(player);
		}
	}

	/// <summary>
	/// Switches the pickup mode for the InteractableStorage in the players hands
	/// TODO should probably be turned into some kind of UIAction component which can hold all these functions
	/// </summary>
	[Command]
	public void CmdSwitchPickupMode()
	{
		if (itemStorage == null) return;
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
			|| playerScript.Mind == null || playerScript.Mind.Body == null)
		{
			return;
		}

		var body = playerScript.Mind.Body.gameObject;

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

	}

	[Server]
	public void ServerToggleChatIcon(string message, ChatModifier chatModifier, LanguageSO language)
	{
		// Cancel right away if the player cannot speak.
		if ((chatModifier & ChatModifier.Mute) == ChatModifier.Mute) return;

		// If emoting don't do speech bubble
		if ((chatModifier & ChatModifier.Emote) == ChatModifier.Emote) return;

		//Don't do anything with chat icon if player is invisible or not spawned in
		if(playerScript.ObjectPhysics != null && playerScript.ObjectPhysics.IsVisible == false) return;
		if(playerScript.playerHealth != null &&
		   (playerScript.playerHealth.IsDead || playerScript.playerHealth.IsCrit)) return;

		//See if we can even send any bubbles
		if(playerScript.PlayerTypeSettings.SendSpeechBubbleTo == PlayerTypes.None) return;

		var visiblePlayers = OtherUtil.GetVisiblePlayers(gameObject.transform.position);

		foreach (var player in visiblePlayers)
		{
			//See if they can receive our speech bubbles
			if(player.Script.PlayerTypeSettings.ReceiveSpeechBubbleFrom.HasFlag(playerScript.PlayerType) == false) continue;

			//See if they we can send our speech bubbles
			if(playerScript.PlayerTypeSettings.SendSpeechBubbleTo.HasFlag(player.Script.PlayerType) == false) continue;

			//See if we need to scramble the message
			var copiedString = LanguageManager.Scramble(language, player.Script, string.Copy(message));

			ShowChatBubbleMessage.SendTo(player.GameObject, gameObject, copiedString, true);
		}
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
		Occupation NewOccupation = playerScript.Mind.occupation;
		if (occupation != null)
		{
			foreach (var job in OccupationList.Instance.Occupations)
			{
				if (job.name != occupation)
				{
					continue;
				}

				NewOccupation = job;
				break;
			}
		}

		PlayerSpawn.RespawnPlayer(playerScript.Mind, NewOccupation, playerScript.Mind.CurrentCharacterSettings);
	}

	[Server]
	public void ServerRespawnPlayerSpecial(string occupation = null, Vector3Int? spawnPos = null)
	{
		Occupation NewOccupation = playerScript.Mind.occupation;
		if (occupation != null)
		{
			var InNewOccupation = SOAdminJobsList.Instance.GetByName(occupation);
			if (InNewOccupation != null)
			{
				NewOccupation = InNewOccupation;
			}
		}

		PlayerSpawn.RespawnPlayerAt(playerScript.Mind, NewOccupation, playerScript.Mind.CurrentCharacterSettings, spawnPos);
	}



	[Server]
	public void ServerRespawnPlayerAntag(PlayerInfo playerToRespawn, string antagonist)
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

		Loggy.LogWarning($"Antagonist string \"{antagonist}\" not found in {nameof(SOAdminJobsList)}!", Category.Antags);
	}

	[Command]
	public void CmdToggleAllowCloning()
	{
		playerScript.Mind.DenyCloning = !playerScript.Mind.DenyCloning;

		if (playerScript.Mind.DenyCloning)
		{
			Chat.AddExamineMsgFromServer(gameObject, "<color=red>You will no longer be cloned</color>");
		}
		else
		{
			Chat.AddExamineMsgFromServer(gameObject, "<color=red>You can now be cloned</color>");
		}
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
	public void CmdSetActiveHand(uint handID, NamedSlot namedSlot)
	{
		NetworkIdentity hand = null;
		if (handID != 0 && NetworkServer.spawned.TryGetValue(handID, out hand) == false) return;
		// Because Ghosts don't have dynamic item storage
		if (playerScript.DynamicItemStorage == null) return;

		if (handID != 0 && hand != null)
		{
			var slot = playerScript.DynamicItemStorage.GetNamedItemSlot(hand.gameObject, namedSlot);
			if (slot == null) return;
			activeHand = hand.gameObject;
		}
		else
		{
			activeHand = null;
		}
		CurrentActiveHand = namedSlot;

	}

	[Command]
	public void CmdPoint(GameObject pointTarget, Vector3 mousePos)
	{
		if (Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction) == false)
			return;
		if (playerScript.IsNormal == false || playerScript.playerHealth.ConsciousState != ConsciousState.CONSCIOUS)
			return;

		if(pointTarget == null) return;

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

	// If we end up needing more information to send to server,
	// probably best to create a new interaction type and use IF2.
	[Command]
	public void CmdRequestSpell(int spellIndex, Vector3 clickPosition)
	{
		foreach (var spell in playerScript.Mind.Spells)
		{
			if (spell.SpellData.Index == spellIndex)
			{
				spell.CallActionServer(PlayerList.Instance.GetOnline(gameObject), clickPosition);
				return;
			}
		}
	}

	[Command]
	public void CmdRequestChangelingAbilites(int abilityIndex, Vector3 clickPosition)
	{
		if (playerScript.Changeling == null)
			return;
		foreach (var ability in playerScript.Changeling.ChangelingAbilities)
		{
			if (ability.AbilityData.Index == abilityIndex)
			{
				ability.CallActionServer(PlayerList.Instance.GetOnline(gameObject), clickPosition);
				return;
			}
		}
	}


	[Command]
	public void CmdRequestChangelingAbilitesWithParam(int abilityIndex, string param)
	{
		if (playerScript.Changeling == null)
			return;
		foreach (var ability in playerScript.Changeling.AbilitiesNow)
		{
			if (ability.AbilityData.Index == abilityIndex)
			{
				ability.CallActionServerWithParam(PlayerList.Instance.GetOnline(gameObject), param);
				return;
			}
		}
	}

	[Command]
	public void CmdRequestChangelingAbilitesToggle(int abilityIndex, bool toggle)
	{
		if (playerScript.Changeling == null)
			return;
		foreach (var ability in playerScript.Changeling.AbilitiesNow)
		{
			if (ability.AbilityData.Index == abilityIndex)
			{
				ability.CallActionServerToggle(PlayerList.Instance.GetOnline(gameObject), toggle);
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

	[Command]
	public void CmdTriggerStorageTrap(GameObject storage)
	{
		//Probably want to put a validations check here to make sure backpack is in range
		//though this is only gonna hurt this player so isnt really hackable lol
		if(storage == null) return;
		if(storage.TryGetComponent<InteractableStorage>(out var interactableStorage) == false) return;

		var slots = interactableStorage.ItemStorage;

		foreach (var slot in slots.GetItemSlots())
		{
			if(slot.IsEmpty) continue;
			if (slot.ItemObject.TryGetComponent<MouseTrap>(out var trap))
			{
				if (trap.IsArmed)
				{
					trap.TriggerTrap(playerScript.playerHealth);
					interactableStorage.PreventUIShowingAfterTrapTrigger = true;
					return;
				}
			}
		}
	}

	[Command]
	public void CmdSetPaintJob(int paintJobIndex)
	{
		var handObject = GetActiveHandItem();

		if (handObject == null || handObject.TryGetComponent<AirlockPainter>(out var painter) == false) return;

		painter.CurrentPaintJobIndex = paintJobIndex;
	}

	[Command]
	public void CmdServerReplaceItemInInventory(GameObject gameObjectSent, uint id, NamedSlot namedSlot)
	{
		if (NetworkServer.spawned.TryGetValue(id, out var replaceItem) == false) return;
		var slot = itemStorage.GetNamedItemSlot(replaceItem.gameObject, namedSlot);
		if (slot == null) return;

		if (gameObjectSent.PickupableOrNull()?.ItemSlot == null) return;
		var fromSlot = gameObjectSent.PickupableOrNull()?.ItemSlot;
		if (fromSlot == null) return;
		if (fromSlot.ItemStorage.ServerIsObserver(playerMove.gameObject))
		{
			Inventory.ServerTransfer(gameObjectSent.PickupableOrNull().ItemSlot, slot, ReplacementStrategy.DropOther);
		}
	}

	[Command]
	public void HardSuicide()
	{
		if (playerScript.TryGetComponent<AiPlayer>(out var aiPlayer))
		{
			aiPlayer.Suicide();
			return;
		}

		if (playerScript.TryGetComponent<BlobPlayer>(out var blobPlayer))
		{
			blobPlayer.Death();
			return;
		}

		var health = playerScript.playerHealth;
		if (health.IsDead)
		{
			Loggy.LogError("[PlayerNetworkActions/HardSuicide()] - Player is already dead!");
			return;
		}
		health.ApplyDamageAll(playerScript.gameObject,
			health.MaxHealth * 2,
			AttackType.Melee, DamageType.Brute,
			false,
			traumaChance: 0);
	}

	[Command]
	public void CmdDoEmote(string emoteName)
	{
		EmoteActionManager.DoEmote(emoteName, playerScript.gameObject);
	}

	[Command]
	public void CmdResetMovementForSelf()
	{
		playerScript.playerMove.ResetEverything();
		playerScript.playerMove.ResetLocationOnClients();
	}

	[Command]
	public void CmdJoinFaith(string faith)
	{
		playerScript.JoinReligion(faith);
	}

	[Command]
	public void CmdSetMainFaith()
	{
		if (FaithManager.Instance.FaithLeaders.Contains(playerScript) == false) return;
		FaithManager.Instance.SetMainFaith(playerScript.CurrentFaith);
		FaithManager.Instance.FaithMembers.Add(playerScript);
	}

	[TargetRpc]
	public void RpcShowFaithSelectScreen(NetworkConnectionToClient target)
	{
		UIManager.Instance.ChaplainFirstTimeSelectScreen.gameObject.SetActive(true);
	}
}
