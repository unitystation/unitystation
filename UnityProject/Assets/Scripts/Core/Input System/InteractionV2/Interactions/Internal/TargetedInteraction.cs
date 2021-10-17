using UnityEngine;

/// <summary>
/// Abstract, only used internally for IF2 - should not be used in interactable components.
/// Represents an interaction that has a target upon which the interaction is performed.
/// </summary>
public abstract class TargetedInteraction: Interaction
{
	/// <summary>Object that is targeted by the interaction. Null if clicking on open space.</summary>
	public GameObject TargetObject { get; protected set; }

	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="usedObject">Object that is being used</param>
	/// <param name="targetObject">Object that is being targeted</param>
	public TargetedInteraction(GameObject performer, GameObject usedObject, GameObject targetObject, Intent intent) :
			base(performer, usedObject, intent)
	{
		TargetObject = targetObject;
	}
}
