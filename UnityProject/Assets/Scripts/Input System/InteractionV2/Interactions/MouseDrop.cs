
using UnityEngine;

/// <summary>
/// Encapsulates all of the info needed for handling a mouse drag and drop interaction
/// </summary>
public class MouseDrop : TargetedInteraction
{
	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="droppedObject">Object that was being dragged and is now being dropped</param>
	/// <param name="targetObject">Object that the dropped object is being dropped on</param>
	public MouseDrop(GameObject performer, GameObject droppedObject, GameObject targetObject) :
		base(performer, droppedObject, targetObject)
	{
	}
}
