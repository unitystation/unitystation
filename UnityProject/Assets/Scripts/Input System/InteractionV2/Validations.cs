
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
	/// Checks if the game object is the specified tool type
	/// </summary>
	/// <param name="toCheck">object to check, can be null</param>
	/// <param name="expectedType"></param>
	/// <returns>true iff toCheck not null and has the Tool component with the specified tool type</returns>
	public static bool IsTool(GameObject toCheck, ToolType expectedType)
	{
		if (toCheck == null) return false;
		var tool = toCheck.GetComponent<Tool>();
		if (tool == null) return false;
		return tool.ToolType == expectedType;
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



	/// <summary>
	/// Checks if a player is allowed to interact with things (based on this player's status, such
	/// as being conscious).
	///
	/// This should be used instead of playerScript.canNotInteract as it handles more possible situations.
	/// </summary>
	/// <param name="player">player gameobject to check</param>
	/// <param name="side">side of the network the check is being performed on</param>
	/// <param name="allowSoftCrit">whether interaction should be allowed if in soft crit</param>
	/// <returns></returns>
	public static bool CanInteract(GameObject player, NetworkSide side, bool allowSoftCrit = false)
	{
		var playerScript = player.GetComponent<PlayerScript>();
		if (playerScript.IsGhost || playerScript.canNotInteract() && (!playerScript.playerHealth.IsSoftCrit || !allowSoftCrit))
		{
			return false;
		}

		return true;
	}

	#region CanApply

	/// <summary>
	/// Validates if the performer is in range and not in crit, which are typical requirements for all
	/// various interactions. Works properly even if player is hidden in a ClosetControl. Can also optionally allow soft crit.
	///
	/// For PositionalHandApply, reach range is based on how far away they are clicking from themselves
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
		var playerScript = player.GetComponent<PlayerScript>();
		var playerObjBehavior = player.GetComponent<ObjectBehaviour>();

		if (!CanInteract(player, side, allowSoftCrit))
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
				targetVector != null ? player.transform.position + targetVector : target.transform.position;
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
						targetVector != null ? player.transform.position + targetVector : target.transform.position;
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
				cnt.ServerState.WorldPosition, player.transform.position, cnt.IsFloatingServer);
		}

		return result;
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

	/// <summary>
	/// Convenience method for implementing server-initiated rollback in WillInteract.
	/// Returns the result of willInteract, but if side == Server and the result is false,
	/// invokes serverRollback first.
	/// </summary>
	/// <param name="interaction"></param>
	/// <param name="side"></param>
	/// <param name="willInteract"></param>
	/// <param name="serverRollback"></param>
	public static bool ValidateWithServerRollback(HandApply interaction, NetworkSide side, Func<HandApply, NetworkSide, bool> willInteract, Action<HandApply> serverRollback)
	{
		var will = willInteract(interaction, side);
		if (!will && side == NetworkSide.Server)
		{
			serverRollback(interaction);
		}

		return will;
	}
}
