
using UnityEngine;

/// <summary>
/// Encapsulates all of the info needed for handling a mouse drag and drop interaction.
/// Invoked for dragging and dropping an object from the world to another object in the world,
/// or an object from inventory to an object in the world. Dragging and dropping between
/// 2 inventory slots is handled by InventoryApply.
/// </summary>
public class MouseDrop : TargetedInteraction
{
	private static readonly MouseDrop Invalid = new MouseDrop(null, null, null, null, Intent.Help);

	/// <summary>
	/// If dragging and dropping from inventory, slot it is being dragged from. Null otherwise.
	/// </summary>
	public readonly ItemSlot FromSlot;

	public bool IsFromInventory => FromSlot != null;

	/// <summary>
	/// Object being dropped (same as UsedObject)
	/// </summary>
	public GameObject DroppedObject => UsedObject;

	/// <summary>
	/// The final location of the shadow object when the interaction was fired.
	/// </summary>
	public Vector2 ShadowWorldLocation;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="droppedObject">Object that was being dragged and is now being dropped</param>
	/// <param name="targetObject">Object that the dropped object is being dropped on</param>
	/// <param name="fromSlot">if dragging from inventory, slot it is being dragged from</param>
	private MouseDrop(GameObject performer, GameObject droppedObject, GameObject targetObject, ItemSlot fromSlot, Intent intent) :
		base(performer, droppedObject, targetObject, intent)
	{
		FromSlot = fromSlot;
	}

	/// <summary>
	/// Create a mouse drop interaction performed by the local player
	/// </summary>
	/// <param name="droppedObject">object being dropped</param>
	/// <param name="targetObject">object being dropped upon</param>
	/// <returns></returns>
	public static MouseDrop ByLocalPlayer(GameObject droppedObject, GameObject targetObject)
	{
		if (PlayerManager.LocalPlayerScript.IsNormal == false) return Invalid;

		var pu = droppedObject.GetComponent<Pickupable>();
		if (pu != null)
		{
			return new MouseDrop(PlayerManager.LocalPlayerObject, droppedObject, targetObject, pu.ItemSlot, UIManager.CurrentIntent);
		}
		else
		{
			return new MouseDrop(PlayerManager.LocalPlayerObject, droppedObject, targetObject, null, UIManager.CurrentIntent);
		}
	}

	/// <summary>
	/// For server only. Create a mouse drop interaction initiated by the client.
	/// </summary>
	/// <param name="clientPlayer">gameobject of the client's player</param>
	/// <param name="droppedObject">object client is dropping (may be currently in inventory).</param>
	/// <param name="targetObject">object being dropped upon.</param>
	/// <returns>a mouse drop by the client, targeting the specified object with the dropped object</returns>
	public static MouseDrop ByClient(GameObject clientPlayer, GameObject droppedObject, GameObject targetObject, Intent intent)
	{
		var pu = droppedObject.GetComponent<Pickupable>();
		if (pu != null)
		{
			return new MouseDrop(clientPlayer, droppedObject, targetObject, pu.ItemSlot, intent);
		}
		else
		{
			return new MouseDrop(clientPlayer, droppedObject, targetObject, null, intent);
		}

	}
}
