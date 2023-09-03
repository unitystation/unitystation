using System;
using System.Collections.Generic;
using UnityEngine;
using TileManagement;
using HealthV2;
using Systems.Ai;
using Systems.Interaction;
using Items;
using Logs;
using Objects.Wallmounts;
using ScriptableObjects;
using Tiles;


// TODO: namespace me to Systems.Interaction (have fun)
/// <summary>
/// Util class containing validation logic you might want to use when
/// developing interactable components. All methods should be designed to work correctly
/// based on whether they are invoked from client or server side (as specified by the NetworkSide parameter).
/// You can use this as a shorthand for the various Validation
/// classes.
/// </summary>
public static class Validations
{
	public const float TELEKINESIS_INTERACTION_DISTANCE = 15f;

	private static readonly List<LayerType> BlockedLayers = new List<LayerType>
		{LayerType.Walls, LayerType.Windows, LayerType.Grills};

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

	public static PlayerTypes CheckState(Predicate<PlayerTypeSettings> toCheck)
	{
		return PlayerTypeSingleton.Instance.DoCheck(toCheck);
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
	public static bool HasItemTrait(Interaction interaction, ItemTrait expectedTrait)
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
	/// <param name="playerScript">playerscript of the player to check</param>
	/// <param name="side">side of the network the check is being performed on</param>
	/// <param name="allowSoftCrit">whether interaction should be allowed if in soft crit</param>
	/// <param name="allowCuffed">whether interaction should be allowed if cuffed</param>
	/// <param name="apt"> the allowed PlayerTypes for this interaction, defaults to normal players</param>
	/// <returns></returns>
	public static bool CanInteract(PlayerScript playerScript, NetworkSide side, bool allowSoftCrit = false, bool allowCuffed = false,
		PlayerTypes apt = PlayerTypes.Normal)
	{
		if (playerScript == null) return false;

		//Only allow players interact this way if contained in allowedPlayerStates (usually only normal players not ghost etc)
		//Note that Ai has AiActivate as that has additional validations
		if (apt.HasFlag(playerScript.PlayerType) == false) return false;

		//Can't interact cuffed
		if (allowCuffed == false && playerScript.playerMove.IsCuffed) return false;

		if (playerScript.playerMove.AllowInput == false) return false;

		if (CanInteractByConsciousState(playerScript.playerHealth, allowSoftCrit, side) == false) return false;

		return true;
	}

	private static bool CanInteractByConsciousState(PlayerHealthV2 playerHealth, bool allowSoftCrit, NetworkSide side)
	{
		if (side == NetworkSide.Client)
		{
			//we only know our own conscious state, so assume true if it's not our local player
			if (playerHealth.gameObject != PlayerManager.LocalPlayerObject) return true;
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
	/// <param name="targetPosition"></param>
	/// <param name="targetVector">target vector pointing from performer to the position they are trying to click,
	/// if specified will use this to determine if in range rather than target object position.</param>
	/// <param name="targetRegisterTile">target's register tile component. If you specify this it avoids garbage. Please provide this
	/// if you can do so without using GetComponent, this is an optimization so GetComponent call can be avoided to avoid
	/// creating garbage.</param>
	/// <param name="apt"> allowedPlayerTypes the allowed PlayerTypes for this interaction, defaults to normal players</param>
	/// <returns></returns>
	public static bool CanApply(
		PlayerScript playerScript,
		GameObject target,
		NetworkSide side,
		bool allowSoftCrit = false,
		ReachRange reachRange = ReachRange.Standard,
		Vector2? targetPosition = null,
		Vector2? targetVector = null,
		RegisterTile targetRegisterTile = null,
		PlayerTypes apt = PlayerTypes.Normal
	)
	{
		if (playerScript == null) return false;

		var playerObjBehavior = playerScript.ObjectPhysics;


		if (CanInteract(playerScript, side, allowSoftCrit, apt: apt) == false)
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
				var parentObj = playerObjBehavior.ContainedInObjectContainer != null
					? playerObjBehavior.ContainedInObjectContainer.gameObject
					: null;
				return parentObj == target;
			}
		}

		var result = false;

		// Check if target is in player's inventory.
		// This was added so NetTabs (NetTab.ValidatePeepers()) can be used on items in an inventory.
		if (target != null && target.TryGetComponent(out Pickupable pickupable) && pickupable.ItemSlot != null)
		{
			if (pickupable.ItemSlot.RootPlayer().OrNull()?.gameObject == playerScript.gameObject)
			{
				result = true;
			}
		}
		else if (reachRange == ReachRange.Unlimited)
		{
			result = true;
		}
		else if (reachRange == ReachRange.Standard)
		{
			result = IsInReachInternal(playerScript, target, side, targetPosition, targetRegisterTile, targetVector: targetVector);
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
				UniversalObjectPhysics uop = (target == null) ? null : target.GetComponent<UniversalObjectPhysics>();

				if (uop == null)
				{
					result = IsInReachInternal(playerScript, target, side, targetPosition, targetRegisterTile, targetVector: targetVector);
				}
				else
				{
					result = ServerCanReachExtended(playerScript, uop);
				}
			}
		}
		else if (reachRange == ReachRange.Telekinesis)
		{
			if ((playerScript.gameObject.AssumedWorldPosServer() - target.AssumedWorldPosServer()).magnitude >
			    TELEKINESIS_INTERACTION_DISTANCE)
			{
				result = false;
			}
			else
			{
				result = true;
			}
		}

		if (result == false && side == NetworkSide.Server && Loggy.LogLevel >= LogLevel.Trace)
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

				if (target.TryGetComponent(out UniversalObjectPhysics uop))
				{
					worldPosition = uop.OfficialPosition;
					isFloating = uop.IsCurrentlyFloating;
				}
			}

			Loggy.LogTraceFormat($"Not in reach! Target: {targetName} server pos:{worldPosition} "+
				                  $"Player Name: {playerScript.playerName} Player pos:{playerScript.RegisterPlayer.WorldPositionServer} " +
								  $"(floating={isFloating})", Category.Exploits);
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
	private static bool IsInReachInternal(PlayerScript playerScript, GameObject target, NetworkSide side, Vector2? TargetPosition,
		RegisterTile targetRegisterTile, Vector2? targetVector = null)
	{
		bool result;
		if (TargetPosition == null && targetVector == null )
		{
			var regTarget = targetRegisterTile == null ? (target == null ? null : target.RegisterTile()) : targetRegisterTile;
			//Use the smart range check which works better on moving matrices
			if (regTarget != null)
			{
				result = IsReachableByRegisterTiles(playerScript.RegisterPlayer, regTarget, side == NetworkSide.Server, context: target);
			}
			else
			{
				//use transform position because we don't have a registered position for the target,
				//this should happen almost never
				//note: we use transform position for both player and target (rather than registered position) because
				//registered position and transform positions can be out of sync with each other esp. on moving matrices
				if (playerScript == null || target == null) return false;
				result = IsReachableByPositions(playerScript.transform.position, target.transform.position, side == NetworkSide.Server, context: target);
			}

		}
		else
		{
			//use target vector based range check
			Vector3 playerWorldPos = playerScript.WorldPos;
			if (TargetPosition != null)
			{
				result = IsReachableByPositions(playerWorldPos, TargetPosition.Value.To3().ToWorld(playerScript.RegisterPlayer.Matrix), side == NetworkSide.Server, context: target);
			}
			else
			{
				result = IsReachableByPositions(playerWorldPos, playerWorldPos + (Vector3)targetVector, side == NetworkSide.Server, context: target);
			}
		}

		return result;
	}

	/// <summary>
	/// Checks if a delta vector is within interaction distance
	/// </summary>
	/// <param name="targetVector">the delta vector representing how distant the interaction is occurring</param>
	/// <param name="interactDist">the horizontal or vertical distance required for out-of-reach</param>
	/// <returns>true if the x and y distance of interaction are less than interactDist</returns>
	public static bool IsInReachDistanceByDelta(Vector3 targetVector, float interactDist = PlayerScript.INTERACTION_DISTANCE)
	{
		return Mathf.Max( Mathf.Abs(targetVector.x), Mathf.Abs(targetVector.y) ) < interactDist;
	}

	/// <summary>
	/// Checks if a delta vector is within interaction distance
	/// </summary>
	/// <param name="targetVector">the delta vector representing how distant the interaction is occurring</param>
	/// <param name="interactDist">the horizontal or vertical distance required for out-of-reach</param>
	/// <returns>true if the x and y distance of interaction are less than interactDist</returns>
	public static bool IsInReachDistanceByPositions(Vector3 fromWorldPos, Vector3 toWorldPos, float interactDist = PlayerScript.INTERACTION_DISTANCE)
	{
		var targetVector = fromWorldPos - toWorldPos;
		return IsInReachDistanceByDelta(targetVector, interactDist: interactDist);
	}


	/// <summary>
	/// Checks if two position vectors are is within interaction distance AND also there is no blocking element between them
	/// </summary>
	/// <param name="fromWorldPos">One position of the interaction</param>
	/// <param name="interactDist">The Other position of the interaction</param>
	/// <param name="isServer">Whether or not this call is occurring on the server</param>
	/// <param name="context">If not null, will ignore collisions caused by this gameobject</param>
	/// <returns>true if the x and y distance of interaction are less than interactDist and there is no blockage. False otherwise.</returns>
	public static bool IsReachableByPositions(
		Vector3 fromWorldPos,
		Vector3 toWorldPos,
		bool isServer,
		float interactDist = PlayerScript.INTERACTION_DISTANCE,
		GameObject context = null
	)
	{
		if (IsNotBlocked(fromWorldPos, toWorldPos, isServer: isServer, context: context))
		{
			Vector3Int toWorldPosInt = toWorldPos.RoundToInt();

			return IsInReachDistanceByPositions(fromWorldPos, toWorldPos, interactDist: interactDist);
		}

		return false;

	}

	private static bool IsNotBlocked(Vector3 worldPosA, Vector3 worldPosB, bool isServer, GameObject context = null)
	{
		Vector3Int worldPosAInt = Vector3Int.RoundToInt(worldPosA);
		Vector3Int worldPosBInt = Vector3Int.RoundToInt(worldPosB);

		if (worldPosAInt == worldPosBInt)
		{
			return true;
		}

		bool result = MatrixManager.IsPassableAtAllMatrices(worldPosAInt, worldPosBInt, isServer: isServer, collisionType: CollisionType.Click,
			context: context, includingPlayers: false, isReach: true,
			excludeLayers: BlockedLayers,
			onlyExcludeLayerOnDestination: true);

		return result;
	}


	/// <summary>
	/// Smart way to detect reach, supports high speeds in ships. Should use it more!
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="isServer">Whether or not this call is occurring on the server</param>
	/// <param name="interactDist"></param>
	/// <param name="context">If not null, will ignore collisions caused by this gameobject</param>
	/// <returns></returns>
	public static bool IsReachableByRegisterTiles(RegisterTile from, RegisterTile to, bool isServer, float interactDist = PlayerScript.INTERACTION_DISTANCE, GameObject context = null)
	{
		if ( isServer )
		{
			return IsReachableByPositions(from.WorldPositionServer, to.WorldPositionServer, isServer, interactDist, context: context);
		}
		else
		{
			return IsReachableByPositions(from.WorldPosition, to.WorldPosition, isServer, interactDist, context: context);
		}
	}

	private static bool ServerCanReachExtended(PlayerScript ps, UniversalObjectPhysics state, GameObject context = null)
	{
		return ps.IsPositionReachable(state.OfficialPosition, true) || ps.IsPositionReachable(state.OfficialPosition - (Vector3)state.NewtonianMovement, true, 1.75f, context: context);
	}

	//AiActivate Validation
	public static bool CanApply(AiActivate toValidate, NetworkSide side, bool lineCast = true)
	{
		return InternalAiActivate(toValidate, side, lineCast);
	}

	private static bool InternalAiActivate(AiActivate toValidate, NetworkSide side, bool lineCast = true)
	{
		if (side == NetworkSide.Client && PlayerManager.LocalPlayerObject != toValidate.Performer) return false;

		//Performer and target cant be null
		if (toValidate.Performer == null || toValidate.TargetObject == null) return false;

		//Ai's shouldn't be able to interacte with items, only objects
		if (toValidate.TargetObject.GetComponent<ItemAttributesV2>() != null) return false;

		//Has to be Ai to do this interaction
		if(toValidate.Performer.TryGetComponent<AiPlayer>(out var aiPlayer) == false) return false;

		//Only allow interactions if true
		if (aiPlayer.AllowRemoteAction == false)
		{
			if (side == NetworkSide.Client)
			{
				Chat.AddExamineMsgToClient("Intelicard remote interactions have been disabled");
			}

			return false;
		}

		//We should always have a camera location, either core or camera
		if (aiPlayer.CameraLocation == null) return false;

		var cameraPos = aiPlayer.CameraLocation.position;

		//Distance check to make sure its in range, this wont be called for "saved" cameras
		if (Vector2.Distance(cameraPos, toValidate.TargetObject.transform.position) > aiPlayer.InteractionDistance) return false;

		if (lineCast == false)
		{
			return true;
		}

		var endPos = toValidate.TargetObject.transform.position;

		//If wall mount calculate to the tile in front of it instead of the wall it is on
		if (toValidate.TargetObject.TryGetComponent<WallmountBehavior>(out var wall))
		{
			endPos = wall.CalculateTileInFrontPos();
		}

		//Check to see if we can directly hit the target tile
		if (LineCheck(cameraPos, aiPlayer, endPos, side) == false)
		{
			//We didnt hit the target tile so we'll try adding the normalised x or y coords
			//This is done because the raycast can miss stuff which should be in line of sight
			//E.g a door on the same axis as the camera would fail usually
			var normalise = (cameraPos - endPos).normalized;

			//Try x first
			if (normalise.x != 0)
			{
				var newEndPos = endPos;
				newEndPos.x += normalise.x.RoundToLargestInt();

				if (LineCheck(cameraPos, aiPlayer, newEndPos, side))
				{
					//If x passes we dont need to try y
					return true;
				}
			}

			if (normalise.y != 0)
			{
				var newEndPos = endPos;
				newEndPos.y += normalise.y.RoundToLargestInt();

				return LineCheck(cameraPos, aiPlayer, newEndPos, side);
			}

			//All coords missed, therefore shouldn't be able to interact
			return false;
		}

		return true;
	}

	private static bool LineCheck(Vector3 cameraPos, AiPlayer aiPlayer, Vector3 endPos, NetworkSide side)
	{
		//raycast to make sure not hidden
		var linecast = MatrixManager.Linecast(cameraPos, LayerTypeSelection.Walls, null,
			endPos);

		//Visualise the interaction on client
		if (side == NetworkSide.Client)
		{
			aiPlayer.ShowInteractionLine(new []{cameraPos, linecast.ItHit ? linecast.HitWorld : endPos}, linecast.ItHit);
		}

		if (linecast.ItHit && Vector3.Distance(endPos, linecast.HitWorld) > 0.5f) return false;

		return true;
	}

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
	public static bool CanFit(ItemSlot itemSlot, GameObject toCheck, NetworkSide side, bool ignoreOccupied = false, GameObject examineRecipient = null)
	{
		var pu = toCheck.GetComponent<Pickupable>();
		if (pu == null) return false;
		return CanFit(itemSlot, pu, side, ignoreOccupied, examineRecipient);
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
			Loggy.LogError("Cannot put item to slot because the item is null playerScript > " +  playerScript + " itemSlot > " + itemSlot, Category.Inventory);
			return false;
		}
		if (CanInteract(playerScript, side, true) == false)
		{
			Loggy.LogTrace("Cannot put item to slot because the player cannot interact", Category.Inventory);
			return false;
		}
		if (CanFit(itemSlot, toCheck, side, ignoreOccupied, examineRecipient) == false)
		{
			Loggy.LogTraceFormat("Cannot put item to slot because the item {0} doesn't fit in the slot {1}", Category.Inventory,
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
			var registerPlayer = playerScript.RegisterPlayer;
			var playerMove = playerScript.playerMove;
			if (registerPlayer == null || playerMove == null) return false;
			return registerPlayer.IsLayingDown || playerMove.IsCuffed;
		}
		else
		{
			//find their exact conscious state, slipping state, cuffed state
			var playerHealth = playerScript.playerHealth;
			var registerPlayer = playerScript.RegisterPlayer;
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

	/// <summary>
	/// Validates that the player has at least a hand.
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public static bool HasHand(GameObject player)
	{
		if (player.TryGetComponent<LivingHealthMasterBase>(out var health) == false)
		{
			return false;
		}

		return health.HasBodyPart(BodyPartType.LeftArm, true) || health.HasBodyPart(BodyPartType.RightArm, true);
	}

	/// <summary>
	/// Validates that the player has both hands.
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public static bool HasBothHands(GameObject player)
	{
		if (player.TryGetComponent<LivingHealthMasterBase>(out var health) == false)
		{
			return false;
		}

		return health.HasBodyPart(BodyPartType.LeftArm, true) && health.HasBodyPart(BodyPartType.RightArm, true);
	}
}
