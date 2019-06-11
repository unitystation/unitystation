
using UnityEngine;

/// <summary>
/// Encapsulates all of the info needed for handling an inventory apply interaction.
///
/// Like HandApply, but targeting something in the inventory rather than in the world.
/// Triggers when clicking an item in the inventory when the active hand has an item.
/// </summary>
public class InventoryApply : TargetedInteraction
{
	private HandSlot handSlot;
	private InventorySlot targetSlot;

	/// <summary>
	/// slot of the hand that is being used to perform the apply.
	/// </summary>
	public HandSlot HandSlot => handSlot;
	/// <summary>
	/// slot of object that the player is applying the used object to
	/// </summary>
	public InventorySlot TargetSlot => targetSlot;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the InventoryApply</param>
	/// <param name="handObject">Object in the player's active hand. Null if player's hand is empty.</param>
	/// <param name="handSlot">hand slot of handObject</param>
	/// <param name="targetObject">object that the player applying the used object to</param>
	private InventoryApply(GameObject performer, GameObject handObject, InventorySlot targetSlot, HandSlot handSlot) :
		base(performer, handObject, targetSlot.Item)
	{
		this.handSlot = handSlot;
		this.targetSlot = targetSlot;
	}

	/// <summary>
	/// Create a InventoryApply interaction performed by the local player using their active hand
	/// </summary>
	/// <param name="targetObjectSlot">slot of the object that the player is applying the active hand item to</param>
	/// <returns></returns>
	public static InventoryApply ByLocalPlayer(InventorySlot targetObjectSlot)
	{
		return new InventoryApply(PlayerManager.LocalPlayer, UIManager.Hands.CurrentSlot.Item,
			targetObjectSlot, HandSlot.ForName(UIManager.Hands.CurrentSlot.eventName));
	}

	/// <summary>
	/// For server only. Create an InventoryApply interaction initiated by the client.
	/// </summary>
	/// <param name="clientPlayer">gameobject of the client's player</param>
	/// <param name="targetObjectSlot">slot of the object that the player is applying their active
	/// hand to</param>
	/// <param name="handObject">object in the player's active hand. This parameter is used so
	/// it doesn't need to be looked up again, since it already should've been looked up in
	/// the message processing logic. Should match SentByPlayer.Script.playerNetworkActions.GetActiveHandItem().</param>
	/// <param name="handSlot">Player's active hand. This parameter is used so
	/// it doesn't need to be looked up again, since it already should've been looked up in
	/// the message processing logic. Should match SentByPlayer.Script.playerNetworkActions.activeHand.</param>
	/// <returns>a hand apply by the client, targeting the specified object with the item in the active hand</returns>

	public static InventoryApply ByClient(GameObject clientPlayer, InventorySlot targetObjectSlot,
		GameObject handObject, HandSlot handSlot)
	{
		return new InventoryApply(clientPlayer, handObject, targetObjectSlot, handSlot);
	}
}
