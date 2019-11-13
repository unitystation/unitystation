
using UnityEngine;

/// <summary>
/// Encapsulates all of the info needed for handling an inventory apply interaction.
///
/// Like HandApply, but targeting something in the inventory rather than in the world.
/// Triggers when clicking an item in the inventory. Triggers even when active hand has no item.
/// </summary>
public class InventoryApply : TargetedInteraction
{
	private static readonly InventoryApply Invalid = new InventoryApply(null, null, null, null);

	private ItemSlot handSlot;
	private ItemSlot targetSlot;

	/// <summary>
	/// Object being used in hand (same as UsedObject). Returns null if nothing in hand.
	/// </summary>
	public GameObject HandObject => UsedObject;

	/// <summary>
	/// slot of the hand that is being used to perform the apply.
	/// </summary>
	public ItemSlot HandSlot => handSlot;
	/// <summary>
	/// slot of object that the player is applying the used object to
	/// </summary>
	public ItemSlot TargetSlot => targetSlot;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the InventoryApply</param>
	/// <param name="handObject">Object in the player's active hand. Null if player's hand is empty.</param>
	/// <param name="targetSlot">object that the player applying the used object to</param>
	/// <param name="handSlot">hand slot of handObject</param>
	private InventoryApply(GameObject performer, GameObject handObject, ItemSlot targetSlot, ItemSlot handSlot) :
		base(performer, handObject, targetSlot?.ItemObject)
	{
		this.handSlot = handSlot;
		this.targetSlot = targetSlot;
	}

	/// <summary>
	/// Create a InventoryApply interaction performed by the local player using their active hand
	/// </summary>
	/// <param name="targetObjectSlot">slot of the object that the player is applying the active hand item to</param>
	/// <returns></returns>
	public static InventoryApply ByLocalPlayer(ItemSlot targetObjectSlot)
	{
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			return Invalid;
		}
		return new InventoryApply(PlayerManager.LocalPlayer, UIManager.Hands.CurrentSlot.ItemObject,
			targetObjectSlot, UIManager.Hands.CurrentSlot.ItemSlot);
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

	public static InventoryApply ByClient(GameObject clientPlayer, ItemSlot targetObjectSlot,
		GameObject handObject, ItemSlot handSlot)
	{
		return new InventoryApply(clientPlayer, handObject, targetObjectSlot, handSlot);
	}
}
