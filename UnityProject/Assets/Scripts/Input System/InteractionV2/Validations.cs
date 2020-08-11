
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Util class containing validation logic you might want to use when
/// developing interactable components. All methods should be designed to work correctly
/// based on whether they are invoked from client or server side (as specified by the NetworkSide parameter).
/// You can use this as a shorthand for the various Validation
/// classes.
/// </summary>
public static class Validations
{
	//Monitors the time between interactions and limits it by the min cool down time
	private static Dictionary<GameObject, DateTime> playerCoolDown = new Dictionary<GameObject, DateTime>();
	private static Dictionary<GameObject, int> playersMaxClick = new Dictionary<GameObject, int>();
	private static double minCoolDown = 0.1f;
	private static int maxClicks = 5;

	/// <summary>
	/// Check if this game object is not null has the specified component
	/// </summary>
	/// <param name="toCheck">object to check, can be null</param>
	/// <typeparam name="T"></typeparam>
	/// <returns>true iff object not null and has component</returns>
	public static bool HasComponent<T>(GameObject toCheck) where T : Component
	{
		return toCheck != null && toCheck.GetComponent(typeof(T)) != null;
	}

	/// <summary>
	/// Checks if the used game object has the indicated component
	/// </summary>
	/// <param name="interaction"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static bool HasUsedComponent<T>(Interaction interaction) where T : Component
	{
		return HasComponent<T>(interaction.UsedObject);
	}

	/// <inheritdoc cref="ItemAttributes.HasTrait"/>
	/// <param name="toCheck">object to check, can be null</param>
	/// <param name="expectedTrait"></param>
	/// <returns></returns>
	public static bool HasItemTrait(GameObject toCheck, ItemTrait expectedTrait)
	{
		if (toCheck == null) return false;
		var attrs = toCheck.GetComponent<ItemAttributesV2>();
		if (attrs == null) return false;
		return attrs.HasTrait(expectedTrait);
	}

	/// <summary>
	/// Checks if the used object has the indicated trait
	/// </summary>
	/// <param name="interaction"></param>
	/// <param name="expectedTrait"></param>
	/// <returns></returns>
	public static bool HasUsedItemTrait(Interaction interaction, ItemTrait expectedTrait)
	{
		return HasItemTrait(interaction.UsedObject, expectedTrait);
	}

	/// <inheritdoc cref="ItemAttributes.HasAnyTrait"/>
	/// <param name="toCheck">object to check, can be null</param>
	/// <param name="expectedTraits"></param>
	/// <returns></returns>
	public static bool HasAnyTrait(GameObject toCheck, IEnumerable<ItemTrait> expectedTraits)
	{
		if (toCheck == null) return false;
		var attrs = toCheck.GetComponent<ItemAttributesV2>();
		if (attrs == null) return false;
		return attrs.HasAnyTrait(expectedTraits);
	}

	/// <inheritdoc cref="ItemAttributes.HasAllTraits"/>
	/// <param name="toCheck">object to check, can be null</param>
	/// <param name="expectedTraits"></param>
	/// <returns></returns>
	public static bool HasAllTraits(GameObject toCheck, IEnumerable<ItemTrait> expectedTraits)
	{
		if (toCheck == null) return false;
		var attrs = toCheck.GetComponent<ItemAttributesV2>();
		if (attrs == null) return false;
		return attrs.HasAllTraits(expectedTraits);
	}

	/// <summary>
	/// Checks if the two objects occupy the same tile.
	/// </summary>
	/// <param name="obj1"></param>
	/// <param name="obj2"></param>
	/// <returns></returns>
	public static bool ObjectsAtSameTile(GameObject obj1, GameObject obj2)
	{
		return obj1.TileWorldPosition() == obj2.TileWorldPosition();
	}

	public static bool IsAdjacent(GameObject obj1, GameObject obj2)
	{
		var dir1 = obj1.TileWorldPosition();
		var dir2 = obj2.TileWorldPosition();
		if(Mathf.Abs(dir1.x - dir2.x) <= 1 && Mathf.Abs(dir1.y - dir2.y) <= 1)
		{
			return true;
		}
		//TODO: check if there's a one sided window or something alike blocking
		return false;
	}

	/// <summary>
	/// Checks if a player is allowed to interact with things (based on this player's status, such
	/// as being conscious, and not cuffed).
	/// </summary>
	/// <param name="player">player gameobject to check</param>
	/// <param name="side">side of the network the check is being performed on</param>
	/// <param name="allowSoftCrit">whether interaction should be allowed if in soft crit</param>
	/// <param name="allowCuffed">whether interaction should be allowed if cuffed</param>
	/// <returns></returns>
	public static bool CanInteract(GameObject player, NetworkSide side, bool allowSoftCrit = false, bool allowCuffed = false, bool isPlayerClick = true)
	{
		if (player == null) return false;

		return CanInteract(player.GetComponent<PlayerScript>(), side, allowSoftCrit, allowCuffed, isPlayerClick);
	}

	/// <summary>
	/// Checks if a player is allowed to interact with things (based on this player's status, such
	/// as being conscious, and not cuffed).
	/// </summary>
	/// <param name="playerScript">playerscript of the player to check</param>
	/// <param name="side">side of the network the check is being performed on</param>
	/// <param name="allowSoftCrit">whether interaction should be allowed if in soft crit</param>
	/// <param name="allowCuffed">whether interaction should be allowed if cuffed</param>
	/// <returns></returns>
	public static bool CanInteract(PlayerScript playerScript, NetworkSide side, bool allowSoftCrit = false, bool allowCuffed = false, bool isPlayerClick = true)
	{
		if (playerScript == null) return false;
		if (isPlayerClick && !CanInteractByCoolDownState(playerScript.gameObject)) return false;

		if ((!allowCuffed && playerScript.playerMove.IsCuffed) ||
		    playerScript.IsGhost ||
		    !playerScript.playerMove.allowInput ||
		    !CanInteractByConsciousState(playerScript.playerHealth, allowSoftCrit, side))
		{
			return false;
		}

		return true;
	}

	//Monitors the interaction rate of a player. If its too fast we return false
	private static bool CanInteractByCoolDownState(GameObject playerObject)
	{
		if (!playersMaxClick.ContainsKey(playerObject))
		{
			playersMaxClick.Add(playerObject, 0);
		}

		if (!playerCoolDown.ContainsKey(playerObject))
		{
			playerCoolDown.Add(playerObject, DateTime.Now);
			return true;
		}

		var totalSeconds = (DateTime.Now - playerCoolDown[playerObject]).TotalSeconds;
		if(totalSeconds < minCoolDown)
		{
			playersMaxClick[playerObject]++;
			if (playersMaxClick[playerObject] <= maxClicks)
			{
				return true;
			}

			return false;
		}

		playerCoolDown[playerObject] = DateTime.Now;
		playersMaxClick[playerObject] = 0;
		return true;
	}

	private static bool CanInteractByConsciousState(PlayerHealth playerHealth, bool allowSoftCrit, NetworkSide side)
	{
		if (side == NetworkSide.Client)
		{
			//we only know our own conscious state, so assume true if it's not our local player
			if (playerHealth.gameObject != PlayerManager.LocalPlayer) return true;
		}

		return playerHealth.ConsciousState == ConsciousState.CONSCIOUS ||
		       playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS && allowSoftCrit;
	}

	#region CanApply

	/// <summary>
	/// Validates if the performer is in range and capable of interaction -  all the typical requirements for all
	/// various interactions. Works properly even if player is hidden in a ClosetControl. Can also optionally allow soft crit.
	///
	/// </summary>
	/// <param name="playerScript">player script performing the interaction</param>
	/// <param name="target">target object.  Might be null if it's empty space</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <param name="allowSoftCrit">whether to allow interaction while in soft crit</param>
	/// <param name="reachRange">range to allow</param>
	/// <param name="targetVector">target vector pointing from performer to the position they are trying to click,
	/// if specified will use this to determine if in range rather than target object position.</param>
	/// <param name="targetRegisterTile">target's register tile component. If you specify this it avoids garbage. Please provide this
	/// if you can do so without using GetComponent, this is an optimization so GetComponent call can be avoided to avoid
	/// creating garbage.</param>
	/// <returns></returns>
	public static bool CanApply(PlayerScript playerScript, GameObject target, NetworkSide side, bool allowSoftCrit = false,
		ReachRange reachRange = ReachRange.Standard, Vector2? targetVector = null, RegisterTile targetRegisterTile = null, bool isPlayerClick = false)
	{
		if (playerScript == null) return false;

		var playerObjBehavior = playerScript.pushPull;

		if (!CanInteract(playerScript, side, allowSoftCrit, isPlayerClick: isPlayerClick))
		{
			return false;
		}

		//no matter what, if player is in closet, they can only reach the closet
		if (playerScript.IsHidden)
		{
			//Client does not seem to know what they are hidden in (playerObjBehavior.parentContianer is not set clientside),
			//so in this case they simply validate this and defer to the server to decide if it's valid
			//TODO: Correct this if there is a way for client to know their container.
			if (side == NetworkSide.Client)
			{
				return true;
			}
			else
			{
				//server checks if player is trying to click the container they are in.
				var parentObj = playerObjBehavior.parentContainer != null
					? playerObjBehavior.parentContainer.gameObject
					: null;
				return parentObj == target;
			}
		}

		var result = false;
		if (reachRange == ReachRange.Unlimited)
		{
			result = true;
		}
		else if (reachRange == ReachRange.Standard)
		{
			result = IsInReachInternal(playerScript, target, side, targetVector, targetRegisterTile);
		}
		else if (reachRange == ReachRange.ExtendedServer)
		{
			//we don't check range client-side for this case.
			if (side == NetworkSide.Client)
			{
				result = true;
			}
			else
			{
				CustomNetTransform cnt = (target == null) ? null : target.GetComponent<CustomNetTransform>();

				if (cnt == null)
				{
					result = IsInReachInternal(playerScript, target, side, targetVector, targetRegisterTile);
				}
				else
				{
					result = ServerCanReachExtended(playerScript, cnt.ServerState);
				}
			}
		}

		if (!result && side == NetworkSide.Server && Logger.LogLevel >= LogLevel.Trace)
		{
			Vector3 worldPosition = Vector3.zero;
			bool isFloating = false;
			string targetName = string.Empty;

			// Client tried to do something out of range, report it
			// Note : it can be a security incident, but it might also be caused by bugs.
			// TODO: verify after a few rounds in multi-player, if this happens too often,
			// with this log information, try to find the cause and fix it (or fine tune it for less frequent logs).

			if (target != null)
			{
				targetName = target.name;

				if (target.TryGetComponent(out CustomNetTransform cnt))
				{
					worldPosition = cnt.ServerState.WorldPosition;
					isFloating = cnt.IsFloatingServer;
				}
				else if (target.TryGetComponent(out PlayerSync playerSync))
				{
					worldPosition = playerSync.ServerState.WorldPosition;
					isFloating = playerSync.IsWeightlessServer;
				}
			}

			Logger.LogTraceFormat($"Not in reach! Target: {targetName} server pos:{worldPosition} "+
				                  $"Player Name: {playerScript.playerName} Player pos:{playerScript.registerTile.WorldPositionServer} " +
								  $"(floating={isFloating})", Category.Security);
		}

		return result;
	}

	/// <summary>
	/// Figures out what method of reach checking to use based on the parameters.
	/// </summary>
	/// <param name="playerScript"></param>
	/// <param name="target">Target of the interraction.  Can be null if empty space.</param>
	/// <param name="side"></param>
	/// <param name="targetVector"></param>
	/// <param name="targetRegisterTile">target's register tile component. If you specify this it avoids garbage. Please provide this
	/// if you can do so without using GetComponent, this is an optimization so GetComponent call can be avoided to avoid
	/// creating garbage.</param>
	/// <returns></returns>
	private static bool IsInReachInternal(PlayerScript playerScript, GameObject target, NetworkSide side, Vector2? targetVector,
		RegisterTile targetRegisterTile)
	{
		bool result;
		if (targetVector == null)
		{
			var regTarget = targetRegisterTile == null ? (target == null ? null : target.RegisterTile()) : targetRegisterTile;
			//Use the smart range check which works better on moving matrices
			if (regTarget != null)
			{
				result = IsInReach(playerScript.registerTile, regTarget, side == NetworkSide.Server);
			}
			else
			{
				//use transform position because we don't have a registered position for the target,
				//this should happen almost never
				//note: we use transform position for both player and target (rather than registered position) because
				//registered position and transform positions can be out of sync with each other esp. on moving matrices
				if (playerScript == null || target == null) return false;
				result = IsInReach(playerScript.transform.position, target.transform.position);
			}

		}
		else
		{
			//use target vector based range check
			result = IsInReach((Vector3) targetVector);
		}

		return result;
	}

	public static bool IsInReach( Vector3 targetVector, float interactDist = PlayerScript.interactionDistance )
	{
		return Mathf.Max( Mathf.Abs(targetVector.x), Mathf.Abs(targetVector.y) ) < interactDist;
	}

	public static bool IsInReach(Vector3 fromWorldPos, Vector3 toWorldPos, float interactDist = PlayerScript.interactionDistance)
	{
		var targetVector = fromWorldPos - toWorldPos;
		return IsInReach( targetVector );
	}


	/// <summary>
	/// Smart way to detect reach, supports high speeds in ships. Should use it more!
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="isServer"></param>
	/// <param name="interactDist"></param>
	/// <returns></returns>
	public static bool IsInReach(RegisterTile from, RegisterTile to, bool isServer, float interactDist = PlayerScript.interactionDistance)
	{
		if ( isServer )
		{
			return from.Matrix == to.Matrix && IsInReach(from.LocalPositionServer, to.LocalPositionServer, interactDist) ||
			       IsInReach(from.WorldPositionServer, to.WorldPositionServer, interactDist);
		}
		else
		{
			return from.Matrix == to.Matrix && IsInReach(from.LocalPositionClient, to.LocalPositionClient, interactDist) ||
			       IsInReach(from.WorldPositionClient, to.WorldPositionClient, interactDist);
		}
	}

	/// <summary>
	/// Validates if the performer is in range and capable of interaction -  all the typical requirements for all
	/// various interactions. Works properly even if player is hidden in a ClosetControl. Can also optionally allow soft crit.
	///
	/// </summary>
	/// <param name="player">player performing the interaction</param>
	/// <param name="target">target object</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <param name="allowSoftCrit">whether to allow interaction while in soft crit</param>
	/// <param name="reachRange">range to allow</param>
	/// <param name="targetVector">target vector pointing from performer to the position they are trying to click,
	/// if specified will use this to determine if in range rather than target object position.</param>
	/// <param name="targetRegisterTile">target's register tile component. If you specify this it avoids garbage. Please provide this
	/// if you can do so without using GetComponent, especially if you are calling this frequently.
	/// This is an optimization so GetComponent call can be avoided to avoid
	/// creating garbage.</param>
	/// <returns></returns>
	public static bool CanApply(GameObject player, GameObject target, NetworkSide side, bool allowSoftCrit = false,
		ReachRange reachRange = ReachRange.Standard, Vector2? targetVector = null, RegisterTile targetRegisterTile = null, bool isPlayerClick = false)
	{
		if (player == null) return false;
		return CanApply(player.GetComponent<PlayerScript>(), target, side, allowSoftCrit, reachRange, targetVector, targetRegisterTile, isPlayerClick);
	}

	private static bool ServerCanReachExtended(PlayerScript ps, TransformState state)
	{
		return ps.IsInReach(state.WorldPosition, true) || ps.IsInReach(state.WorldPosition - (Vector3)state.WorldImpulse, true, 1.75f);
	}

	/// <summary>
	/// Validates if the performer is in range and not in crit for a HandApply interaction.
	/// </summary>
	/// <param name="toValidate">interaction to validate</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <param name="allowSoftCrit">whether to allow interaction while in soft crit</param>
	/// <param name="reachRange">range to allow</param>
	/// <returns></returns>
	public static bool CanApply(HandApply toValidate, NetworkSide side, bool allowSoftCrit = false, ReachRange reachRange = ReachRange.Standard, bool isPlayerClick = false) =>
		CanApply(toValidate.Performer, toValidate.TargetObject, side, allowSoftCrit, reachRange, isPlayerClick: isPlayerClick);

	/// <summary>
	/// Validates if the performer is in range and not in crit for a PositionalHandApply interaction.
	/// Range check is based on the target vector of toValidate, not the distance to the object.
	/// </summary>
	/// <param name="toValidate">interaction to validate</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <param name="allowSoftCrit">whether to allow interaction while in soft crit</param>
	/// <param name="reachRange">range to allow</param>
	/// <returns></returns>
	public static bool CanApply(PositionalHandApply toValidate, NetworkSide side, bool allowSoftCrit = false, ReachRange reachRange = ReachRange.Standard, bool isPlayerClick = false) =>
		CanApply(toValidate.Performer, toValidate.TargetObject, side, allowSoftCrit, reachRange, toValidate.TargetVector, isPlayerClick: isPlayerClick);

	/// <summary>
	/// Validates if the performer is in range and not in crit for a TileApply interaction.
	/// Range check is based on the target vector of toValidate, not the distance to the object.
	/// </summary>
	/// <param name="toValidate">interaction to validate</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <param name="allowSoftCrit">whether to allow interaction while in soft crit</param>
	/// <param name="reachRange">range to allow</param>
	/// <returns></returns>
	public static bool CanApply(TileApply toValidate, NetworkSide side, bool allowSoftCrit = false, ReachRange reachRange = ReachRange.Standard, bool isPlayerClick = false) =>
		CanApply(toValidate.Performer, toValidate.TargetInteractableTiles.gameObject, side, allowSoftCrit, reachRange, toValidate.TargetVector, isPlayerClick: isPlayerClick);

	/// <summary>
	/// Validates if the performer is in range and not in crit for a MouseDrop interaction.
	/// </summary>
	/// <param name="toValidate">interaction to validate</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <param name="allowSoftCrit">whether to allow interaction while in soft crit</param>
	/// <param name="reachRange">range to allow</param>
	/// <returns></returns>
	public static bool CanApply(MouseDrop toValidate, NetworkSide side, bool allowSoftCrit = false, ReachRange reachRange = ReachRange.Standard, bool isPlayerClick = false) =>
		CanApply(toValidate.Performer, toValidate.TargetObject, side, allowSoftCrit, reachRange, isPlayerClick: isPlayerClick);

	public static bool CanApply(ConnectionApply toValidate, NetworkSide side, bool allowSoftCrit = false, ReachRange reachRange = ReachRange.Standard, bool isPlayerClick = false) =>
		CanApply(toValidate.Performer, toValidate.TargetObject, side, allowSoftCrit, reachRange, toValidate.TargetVector, isPlayerClick: isPlayerClick);

	public static bool CanApply(ContextMenuApply toValidate, NetworkSide side, bool allowSoftCrit = false, ReachRange reachRange = ReachRange.Standard, bool isPlayerClick = false) =>
		CanApply(toValidate.Performer, toValidate.TargetObject, side, allowSoftCrit, reachRange, isPlayerClick: isPlayerClick);
	#endregion


	public static bool IsMineableAt(Vector2 targetWorldPosition, MetaTileMap metaTileMap)
	{
		var wallTile = metaTileMap.GetTileAtWorldPos(targetWorldPosition, LayerType.Walls) as BasicTile;
		return wallTile != null && wallTile.Mineable;
	}

	/// <summary>
	/// Checks if the indicated item can fit in this slot. Correctly handles logic for client / server side, so is
	/// recommended to use in WillInteract rather than other ways of checking fit.
	/// </summary>
	/// <param name="itemSlot">slot to check</param>
	/// <param name="toCheck">item to check for fit</param>
	/// <param name="side">network side check is happening on</param>
	/// <param name="ignoreOccupied">if true, does not check if an item is already in the slot</param>
	/// <returns></returns>
	public static bool CanFit(ItemSlot itemSlot, GameObject toCheck, NetworkSide side, bool ignoreOccupied = false)
	{
		var pu = toCheck.GetComponent<Pickupable>();
		if (pu == null) return false;
		return CanFit(itemSlot, pu, side, ignoreOccupied);
	}

	/// <summary>
	/// Checks if the indicated item can fit in this slot. Correctly handles logic for client / server side, so is
	/// recommended to use in WillInteract rather than other ways of checking fit.
	/// </summary>
	/// <param name="itemSlot">slot to check</param>
	/// <param name="toCheck">item to check for fit</param>
	/// <param name="side">network side check is happening on</param>
	/// <param name="ignoreOccupied">if true, does not check if an item is already in the slot</param>
	/// <param name="examineRecipient">if not null, when validation fails, will output an appropriate examine message to this recipient</param>
	/// <returns></returns>
	public static bool CanFit(ItemSlot itemSlot, Pickupable toCheck, NetworkSide side, bool ignoreOccupied = false, GameObject examineRecipient = null)
	{
		if (itemSlot == null) return false;
		return itemSlot.CanFit(toCheck, ignoreOccupied, examineRecipient);
	}

	/// <summary>
	/// Checks if the player can currently put the indicated item into a free slot in this storage. Correctly handles logic for client / server side, so is
	/// recommended to use in WillInteract rather than other ways of checking fit.
	/// </summary>
	/// <param name="player">player to check</param>
	/// <param name="storage">storage to check</param>
	/// <param name="toCheck">item to check for fit</param>
	/// <param name="side">network side check is happening on</param>
	/// <param name="ignoreOccupied">if true, does not check if an item is already in the slot</param>
	/// <param name="examineRecipient">if not null, when validation fails, will output an appropriate examine message to this recipient</param>
	/// <returns></returns>
	public static bool CanPutItemToStorage(PlayerScript playerScript, ItemStorage storage, Pickupable toCheck,
		NetworkSide side, bool ignoreOccupied = false, GameObject examineRecipient = null)
	{
		var freeSlot = storage.GetBestSlotFor(toCheck);
		if (freeSlot == null) return false;
		return CanPutItemToSlot(playerScript, freeSlot, toCheck, side, ignoreOccupied, examineRecipient);
	}

	/// <summary>
	/// Checks if the player can currently put the indicated item into a free slot in this storage. Correctly handles logic for client / server side, so is
	/// recommended to use in WillInteract rather than other ways of checking fit.
	/// </summary>
	/// <param name="player">player to check</param>
	/// <param name="storage">storage to check</param>
	/// <param name="toCheck">item to check for fit</param>
	/// <param name="side">network side check is happening on</param>
	/// <param name="ignoreOccupied">if true, does not check if an item is already in the slot</param>
	/// <param name="examineRecipient">if not null, when validation fails, will output an appropriate examine message to this recipient</param>
	/// <returns></returns>
	public static bool CanPutItemToStorage(PlayerScript playerScript, ItemStorage storage, GameObject toCheck,
		NetworkSide side, bool ignoreOccupied = false, GameObject examineRecipient = null)
	{
		if (toCheck == null) return false;
		return CanPutItemToStorage(playerScript, storage, toCheck.GetComponent<Pickupable>(), side, ignoreOccupied,
			examineRecipient);
	}

	/// <summary>
	/// Checks if the player can currently put the indicated item in this slot. Correctly handles logic for client / server side, so is
	/// recommended to use in WillInteract rather than other ways of checking fit.
	/// </summary>
	/// <param name="player">player performing the interaction</param>
	/// <param name="itemSlot">slot to check</param>
	/// <param name="toCheck">item to check for fit</param>
	/// <param name="side">network side check is happening on</param>
	/// <param name="ignoreOccupied">if true, does not check if an item is already in the slot</param>
	/// <param name="examineRecipient">if not null, when validation fails, will output an appropriate examine message to this recipient</param>
	/// <returns></returns>
	public static bool CanPutItemToSlot(PlayerScript playerScript, ItemSlot itemSlot, Pickupable toCheck, NetworkSide side,
		bool ignoreOccupied = false, GameObject examineRecipient = null)
	{
		if (toCheck == null)
		{
			Logger.LogError("Cannot put item to slot because the item is null", Category.Inventory);
			return false;
		}
		if (!CanInteract(playerScript.gameObject, side, true))
		{
			Logger.LogTrace("Cannot put item to slot because the player cannot interact", Category.Inventory);
			return false;
		}
		if (!CanFit(itemSlot, toCheck, side, ignoreOccupied, examineRecipient))
		{
			Logger.LogTraceFormat("Cannot put item to slot because the item {0} doesn't fit in the slot {1}", Category.Inventory,
				toCheck.name, itemSlot);
			return false;
		}
		return true;
	}

	/// <summary>
	/// Checks if the player is allowed to have their inventory examined and removed
	/// </summary>
	/// <param name="player"></param>
	/// <param name="side"></param>
	/// <returns></returns>
	public static bool IsStrippable(GameObject player, NetworkSide side)
	{
		if (player == null) return false;
		var playerScript = player.GetComponent<PlayerScript>();
		if (playerScript == null) return false;

		if (side == NetworkSide.Client)
		{
			//we don't know their exact health state and whether they are slipping, but we can guess if they're downed we can do this
			var registerPlayer = playerScript.registerTile;
			var playerMove = playerScript.playerMove;
			if (registerPlayer == null || playerMove == null) return false;
			return registerPlayer.IsLayingDown || playerMove.IsCuffed;
		}
		else
		{
			//find their exact conscious state, slipping state, cuffed state
			var playerHealth = playerScript.playerHealth;
			var registerPlayer = playerScript.registerTile;
			var playerMove = playerScript.playerMove;
			if (playerHealth == null || playerMove == null || registerPlayer == null) return false;
			return playerHealth.ConsciousState != ConsciousState.CONSCIOUS || registerPlayer.IsSlippingServer || playerMove.IsCuffed;
		}
	}

	/// <summary>
	/// Returns true iff both objects are Stackable and toAdd can be added to stack.
	/// </summary>
	public static bool CanStack(GameObject stack, GameObject toAdd)
	{
		if (stack == null || toAdd == null) return false;
		var stack1 = stack.GetComponent<Stackable>();
		var stack2 = toAdd.GetComponent<Stackable>();
		if (stack1 == null || stack2 == null) return false;

		return stack1.CanAccommodate(stack2);
	}

	/// <summary>
	/// Checks if the provided object is stackable and has the required minimum amount in the stack.
	/// </summary>
	/// <param name="stack"></param>
	/// <param name="minAmount"></param>
	/// <returns></returns>
	public static bool HasAtLeast(GameObject stack, int minAmount)
	{
		if (stack == null) return false;
		var stackable = stack.GetComponent<Stackable>();
		if (stackable == null) return false;
		return stackable.Amount >= minAmount;
	}

	/// <summary>
	/// Checks if the used object is stackable and has the required minimum amount in the stack.
	/// </summary>
	/// <param name="stack"></param>
	/// <param name="minAmount"></param>
	/// <returns></returns>
	public static bool HasUsedAtLeast(Interaction interaction, int minAmount)
	{
		return HasAtLeast(interaction.UsedObject, minAmount);
	}

	/// <summary>
	/// Checks if the indicated game object is the target.
	/// </summary>
	/// <param name="gameObject"></param>
	/// <param name="interaction"></param>
	/// <returns></returns>
	public static bool IsTarget(GameObject gameObject, TargetedInteraction interaction)
	{
		return gameObject == interaction.TargetObject;
	}

	/// <summary>
	/// Checks if a welder which is on is being used
	/// </summary>
	/// <param name="interaction"></param>
	/// <returns></returns>
	public static bool HasUsedActiveWelder(Interaction interaction)
	{
		if (interaction.UsedObject == null) return false;
		var welder = interaction.UsedObject.GetComponent<Welder>();
		if (welder == null) return false;
		return welder.IsOn;
	}

	public static bool HasTarget(TargetedInteraction interaction)
	{
		return interaction.TargetObject != null;
	}
}
