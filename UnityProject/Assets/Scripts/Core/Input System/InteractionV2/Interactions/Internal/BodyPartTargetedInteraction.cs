using UnityEngine;

/// <summary>
/// Abstract, only used internally for IF2 - should not be used in interactable components.
/// Targeted interaction which targets a specific body part
/// </summary>
public abstract class BodyPartTargetedInteraction: TargetedInteraction
{
	/// <summary>Body part being targeted.</summary>
	public BodyPartType TargetBodyPart { get; protected set; }

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="usedObject">Object that is being used</param>
	/// <param name="targetObject">Object that is being targeted</param>
	/// <param name="targetBodyPart">targeted body part</param>
	public BodyPartTargetedInteraction(GameObject performer, GameObject usedObject, GameObject targetObject, BodyPartType targetBodyPart, Intent intent) :
		base(performer, usedObject, targetObject, intent)
	{
		TargetBodyPart = targetBodyPart;
	}
}
