
using System;
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
	/// Checks if the game object has the specified trait
	/// </summary>
	/// <param name="toCheck">object to check, can be null</param>
	/// <param name="expectedTrait"></param>
	/// <returns></returns>
	public static bool HasItemTrait(GameObject toCheck, ItemTrait expectedTrait)
	{
		if (toCheck == null) return false;
		var attrs = toCheck.GetComponent<IItemAttributes>();
		if (attrs == null) return false;
		return attrs.HasTrait(expectedTrait);
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
	public static bool CanInteract(GameObject player, NetworkSide side, bool allowSoftCrit = false, bool allowCuffed = false)
	{
		if (player == null) return false;
		return CanInteract(player.GetComponent<PlayerScript>(), side, allowSoftCrit, allowCuffed);
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
	public static bool CanInteract(PlayerScript playerScript, NetworkSide side, bool allowSoftCrit = false, bool allowCuffed = false)
	{
		if (playerScript == null) return false;
		if ((!allowCuffed && playerScript.playerMove.IsCuffed) ||
		    playerScript.IsGhost ||
		    !playerScript.playerMove.allowInput ||
		    !CanInteractByConsciousState(playerScript.playerHealth, allowSoftCrit, side))
		{
			return false;
		}

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
	/// <param name="target">target object</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <param name="allowSoftCrit">whether to allow interaction while in soft crit</param>
	/// <param name="reachRange">range to allow</param>
	/// <param name="targetVector">target vector pointing from performer to the position they are trying to click,
	/// if specified will use this to determine if in range rather than target object position.</param>
	/// <returns></returns>
	public static bool CanApply(PlayerScript playerScript, GameObject target, NetworkSide side, bool allowSoftCrit = false,
		ReachRange reachRange = ReachRange.Standard, Vector2? targetVector = null)
	{
		if (playerScript == null) return false;
		var playerObjBehavior = playerScript.pushPull;

		if (!CanInteract(playerScript, side, allowSoftCrit))
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
			var targetWorldPosition =
				targetVector != null ? playerScript.transform.position + targetVector : target.transform.position;
			result = playerScript.IsInReach((Vector3) targetWorldPosition, side == NetworkSide.Server);
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
				var cnt = target.GetComponent<CustomNetTransform>();
				if (cnt == null)
				{
					var targetWorldPosition =
						targetVector != null ? playerScript.transform.position + targetVector : target.transform.position;
					//fallback to standard range check if there is no CNT
					result = playerScript.IsInReach((Vector3) targetWorldPosition, side == NetworkSide.Server);
				}
				else
				{
					result = ServerCanReachExtended(playerScript, cnt.ServerState);
				}
			}
		}

		if (!result && side == NetworkSide.Server)
		{
			//client tried to do something out of range, report it
			var cnt = target.GetComponent<CustomNetTransform>();
			Logger.LogTraceFormat( "Not in reach! server pos:{0} player pos:{1} (floating={2})", Category.Security,
				cnt.ServerState.WorldPosition, playerScript.transform.position, cnt.IsFloatingServer);
		}

		return result;
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
	/// <returns></returns>
	public static bool CanApply(GameObject player, GameObject target, NetworkSide side, bool allowSoftCrit = false,
		ReachRange reachRange = ReachRange.Standard, Vector2? targetVector = null)
	{
		if (player == null) return false;
		return CanApply(player.GetComponent<PlayerScript>(), target, side, allowSoftCrit, reachRange, targetVector);
	}

	private static bool ServerCanReachExtended(PlayerScript ps, TransformState state)
	{
		return ps.IsInReach(state.WorldPosition, true) || ps.IsInReach(state.WorldPosition - (Vector3)state.Impulse, true, 1.75f);
	}

	/// <summary>
	/// Validates if the performer is in range and not in crit for a HandApply interaction.
	/// </summary>
	/// <param name="toValidate">interaction to validate</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <param name="allowSoftCrit">whether to allow interaction while in soft crit</param>
	/// <param name="reachRange">range to allow</param>
	/// <returns></returns>
	public static bool CanApply(HandApply toValidate, NetworkSide side, bool allowSoftCrit = false, ReachRange reachRange = ReachRange.Standard) =>
		CanApply(toValidate.Performer, toValidate.TargetObject, side, allowSoftCrit, reachRange);

	/// <summary>
	/// Validates if the performer is in range and not in crit for a PositionalHandApply interaction.
	/// Range check is based on the target vector of toValidate, not the distance to the object.
	/// </summary>
	/// <param name="toValidate">interaction to validate</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <param name="allowSoftCrit">whether to allow interaction while in soft crit</param>
	/// <param name="reachRange">range to allow</param>
	/// <returns></returns>
	public static bool CanApply(PositionalHandApply toValidate, NetworkSide side, bool allowSoftCrit = false, ReachRange reachRange = ReachRange.Standard) =>
		CanApply(toValidate.Performer, toValidate.TargetObject, side, allowSoftCrit, reachRange, toValidate.TargetVector);

	/// <summary>
	/// Validates if the performer is in range and not in crit for a MouseDrop interaction.
	/// </summary>
	/// <param name="toValidate">interaction to validate</param>
	/// <param name="side">side of the network this is being checked on</param>
	/// <param name="allowSoftCrit">whether to allow interaction while in soft crit</param>
	/// <param name="reachRange">range to allow</param>
	/// <returns></returns>
	public static bool CanApply(MouseDrop toValidate, NetworkSide side, bool allowSoftCrit = false, ReachRange reachRange = ReachRange.Standard) =>
		CanApply(toValidate.Performer, toValidate.TargetObject, side, allowSoftCrit, reachRange);

	#endregion


	public static bool IsMineableAt(Vector2 targetWorldPosition, MetaTileMap metaTileMap)
	{
		var wallTile = metaTileMap.GetTileAtWorldPos(targetWorldPosition, LayerType.Walls);
		if (wallTile == null) return false;
		if (!(wallTile is BasicTile)) return false;

		var basicWallTile = wallTile as BasicTile;
		return basicWallTile.Mineable;
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
		if (toCheck == null || itemSlot.Item != null)
		{
			Logger.LogTrace("Cannot put item to slot because the item or slot are null", Category.Inventory);
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
			return registerPlayer.IsDown || playerMove.IsCuffed;
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
}
