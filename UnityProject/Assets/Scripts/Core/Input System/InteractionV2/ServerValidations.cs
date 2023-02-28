
using System;
using System.Collections.Generic;
using System.Linq;
using Systems.Character;
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



	/// <summary>
	/// Validates that the player's character name.
	/// </summary>
	/// <param name="characterName">CharacterSettings.Name</param>
	/// <returns>True if illegal.</returns>
	public static bool HasIllegalCharacterName(String characterName)
	{
		if(characterName.Any(char.IsDigit) || characterName.Any(char.IsSymbol)
		|| characterName.Count() > GameManager.Instance.CharacterNameLimit || characterName.Contains("\n")
		|| characterName.All(char.IsUpper))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// Validates that the player's character age.
	/// </summary>
	/// <param name="characterName">CharacterSettings.Age</param>
	/// <returns>True if illegal.</returns>
	public static bool HasIllegalCharacterAge(int characterAge)
	{
		if(characterAge > 78 || characterAge <= 17)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// Validates that the player's skintone.
	/// </summary>
	/// <param name="settings">The character sheet used to check what race the player is.</param>
	/// <returns>True if illegal.</returns>
	public static bool HasIllegalSkinTone(CharacterSheet settings)
	{
		PlayerHealthData SetRace = null;
		bool skinToneIsIllegal = false;
		SetRace = settings.GetRaceSo();
		List<Color> availableSkinColors = SetRace.Base.SkinColours;
		Color currentSkinColor;
		ColorUtility.TryParseHtmlString(settings.SkinTone, out currentSkinColor);
		if(availableSkinColors.Count > 0)
		{
			foreach (var skinColour in availableSkinColors)
			{
				if (Math.Abs(skinColour.a - currentSkinColor.a) < 0.01f
				    && Math.Abs(skinColour.r - currentSkinColor.r) < 0.01f
				    && Math.Abs(skinColour.g - currentSkinColor.g) < 0.01f
				    && Math.Abs(skinColour.b - currentSkinColor.b) < 0.01f)
				{
					skinToneIsIllegal = false;
					break;
				}
				else
				{
					skinToneIsIllegal = true;
				}
			}
		}
		else
		{
			return false;
		}
		if(skinToneIsIllegal == false)
		{
			if(currentSkinColor.a <= 0.99f)
			{
				skinToneIsIllegal = true;
			}
		}

		return skinToneIsIllegal;
	}

}
