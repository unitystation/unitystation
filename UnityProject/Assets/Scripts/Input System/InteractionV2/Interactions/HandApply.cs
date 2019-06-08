
using UnityEngine;

/// <summary>
/// Encapsulates all of the info needed for handling a hand apply interaction.
///
/// A hand apply interaction occurs when a player clicks something in the game world. The object
/// in their hand (or their empty hand) is applied to the target object.
/// </summary>
public class HandApply : TargetedInteraction
{
	private readonly string handSlotName;

	public string HandSlotName => handSlotName;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the drop interaction</param>
	/// <param name="handObject">Object in the player's hand. Null if player's hand is empty.</param>
	/// <param name="targetObject">Object that the player clicked on</param>
	/// <param name="handSlotName">name of the hand slot that is being used.</param>
	public HandApply(GameObject performer, GameObject handObject, GameObject targetObject, string handSlotName) :
		base(performer, handObject, targetObject)
	{
		this.handSlotName = handSlotName;
	}

	/// <summary>
	/// Creates a HandApply interaction performed by the local player, on client side, targeting the specified object.
	/// </summary>
	/// <param name="targetObject">object targeted by the interaction</param>
	/// <returns></returns>
	public static HandApply ByLocalPlayer(GameObject targetObject)
	{
		return new HandApply(PlayerManager.LocalPlayer,
			UIManager.Hands.CurrentSlot.Item,
			targetObject,
			UIManager.Instance.hands.CurrentSlot.eventName);
	}
}
