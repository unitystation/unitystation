
using UnityEngine;

/// <summary>
/// Represents an interaction that has a target upon which the interaction is performed.
/// </summary>
public abstract class TargetedInteraction: Interaction
{
	private readonly GameObject targetObject;

	/// <summary>
	/// Object that is targeted by the interaction
	/// </summary>
	public GameObject TargetObject => targetObject;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="usedObject">Object that is being used</param>
	/// <param name="targetObject">Object that is being targeted</param>
	public TargetedInteraction(GameObject performer, GameObject usedObject, GameObject targetObject) :
		base(performer, usedObject)
	{
		this.targetObject = targetObject;
	}
}
