
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Contains validation logic that can only be used on the server.
/// Can add convenience logic such as notifying the player why they can't do something.
/// </summary>
public static class ServerValidations
{
	/// <summary>
	/// Checks if the position is blocked by anything that would prevent construction or anchoring.
	/// If blocked, optionally messages the performer telling them what's in the way.
	/// Valid on server only.
	/// </summary>
	/// <param name="performer">player performing trying to perform the action, will message if </param>
	/// <param name="anchoredObject">object being anchored, null if constructing a new object </param>
	/// <param name="worldPosition">world position the construction is attempted on </param>
	/// <param name="allowed">If defined, will be used to check each other registertile at the position. It should return
	/// true if this object is allowed to be built / anchored on top of the given register tile, otherwise false.
	/// If unspecified, all non-floor registertiles will be considered as blockers at the indicated position</param>
	public static bool IsConstructionBlocked(GameObject performer, GameObject anchoredObject, Vector2Int worldPosition, Func<RegisterTile, bool> allowed = null,
		bool messagePerformer = true)
	{
		var floorLayer = LayerMask.NameToLayer("Floor");
		var wallmountLayer = LayerMask.NameToLayer("WallMounts");
		var itemsLayer = LayerMask.NameToLayer("Items");
		var machinesLayer = LayerMask.NameToLayer("Machines");
		var lightingLayer = LayerMask.NameToLayer("Lighting");
		//blood splat layer is default
		var defaultLayer = LayerMask.NameToLayer("Default");
		if (allowed == null) allowed = (rt) => false;
		var blocker =
			MatrixManager.GetAt<RegisterTile>(worldPosition.To3Int(), true)
				//ignore the object itself (if anchoring)
				.Where(rt => rt.gameObject != anchoredObject)
				//ignore performer
				.Where(rt => rt.gameObject != performer)
				//ignore stuff in floor and wallmounts
				.Where(rt => rt.gameObject.layer != floorLayer &&
				rt.gameObject.layer != wallmountLayer &&
				rt.gameObject.layer != itemsLayer &&
				rt.gameObject.layer != machinesLayer &&
				rt.gameObject.layer != lightingLayer &&
				rt.gameObject.layer != defaultLayer)
				.FirstOrDefault(rt => !allowed.Invoke(rt));
		if (blocker != null)
		{
			//cannot build if there's anything in the way (other than the builder).
			if (messagePerformer)
			{
				Chat.AddExamineMsg(performer,
					$"{blocker.gameObject.ExpensiveName()} is in the way.");
			}
			return true;
		}

		return false;
	}

	/// <summary>
	/// Checks if the object targeted by the interaction is blocked by anything that would prevent anchoring.
	/// Optionally messages performer telling them why its blocked if it is blocked.
	/// </summary>
	/// <param name="handApply">interaction attempting to anchor the object.</param>
	/// <param name="allowed">If defined, will be used to check each other registertile at the position. It should return
	/// true if this object is allowed to be anchored on top of the given register tile, otherwise false.
	/// If unspecified, all non-floor registertiles will be considered as blockers at the indicated position</param>
	public static bool IsAnchorBlocked(HandApply handApply,  Func<RegisterTile, bool> allowed = null, bool messagePerformer = true)
	{
		return IsConstructionBlocked(handApply.Performer, handApply.TargetObject,
			handApply.TargetObject.TileWorldPosition(), allowed, messagePerformer);
	}

}
