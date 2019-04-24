
using UnityEngine;

/// <summary>
/// Encapsulates all of the info needed for handling a hand apply interaction.
///
/// A hand apply interaction occurs when a player clicks something in the game world. The object
/// in their hand (or their empty hand) is applied to the target object.
/// </summary>
public class HandApply : TargetedInteraction
{
	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="handObject">Object in the player's hand. Null if player's hand is empty.</param>
	/// <param name="targetObject">Object that the player clicked on</param>
	public HandApply(GameObject performer, GameObject handObject, GameObject targetObject) :
		base(performer, handObject, targetObject)
	{
	}
}
