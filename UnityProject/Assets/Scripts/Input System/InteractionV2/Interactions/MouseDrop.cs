
using UnityEngine;

/// <summary>
/// Encapsulates all of the info needed for handling a mouse drag and drop interaction
/// </summary>
public class MouseDrop : TargetedInteraction
{

	/// <summary>
	/// Object being dropped (same as UsedObject)
	/// </summary>
	public GameObject DroppedObject => UsedObject;
	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="droppedObject">Object that was being dragged and is now being dropped</param>
	/// <param name="targetObject">Object that the dropped object is being dropped on</param>
	private MouseDrop(GameObject performer, GameObject droppedObject, GameObject targetObject) :
		base(performer, droppedObject, targetObject)
	{
	}

	/// <summary>
	/// Create a mouse drop interaction performed by the local player
	/// </summary>
	/// <param name="droppedObject">object being dropped</param>
	/// <param name="targetObject">object being dropped upon</param>
	/// <returns></returns>
	public static MouseDrop ByLocalPlayer(GameObject droppedObject, GameObject targetObject)
	{
		return new MouseDrop(PlayerManager.LocalPlayer, droppedObject, targetObject);
	}

	/// <summary>
	/// For server only. Create a mouse drop interaction initiated by the client.
	/// </summary>
	/// <param name="clientPlayer">gameobject of the client's player</param>
	/// <param name="droppedObject">object client is dropping.</param>
	/// <param name="targetObject">object being dropped upon.</param>
	/// <returns>a mouse drop by the client, targeting the specified object with the dropped object</returns>
	public static MouseDrop ByClient(GameObject clientPlayer, GameObject droppedObject, GameObject targetObject)
	{
		return new MouseDrop(clientPlayer, droppedObject, targetObject);
	}
}
