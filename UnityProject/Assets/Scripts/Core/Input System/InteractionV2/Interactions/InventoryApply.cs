
using UnityEngine;

/// <summary>
/// Encapsulates all of the info needed for handling an inventory apply interaction.
///
/// Triggered by clicking an inventory slot (in which case the active hand will be the
/// from slot even if it's empty) or dragging from one slot to another.
/// </summary>
public class InventoryApply : TargetedInteraction
{
	private static readonly InventoryApply Invalid = new InventoryApply(null, null, null, Intent.Help, false);

	private ItemSlot fromSlot;
	private ItemSlot targetSlot;

	/// <summary>
	/// slot of the hand that is being used to perform the apply, or
	/// slot the item was dragged from if dragging from one slot to another.
	/// </summary>
	public ItemSlot FromSlot => fromSlot;
	/// <summary>
	/// slot of object that the player is applying the used object to
	/// </summary>
	public ItemSlot TargetSlot => targetSlot;

	/// <summary>
	/// True iff the FromSlot is one of the performer's hands
	/// </summary>
	public bool IsFromHandSlot => fromSlot.ItemStorage.Player.OrNull()?.gameObject == Performer &&
	                              (fromSlot.SlotIdentifier.NamedSlot == NamedSlot.leftHand ||
	                              fromSlot.SlotIdentifier.NamedSlot == NamedSlot.rightHand);

	/// <summary>
	/// True iff the target slot is one of the performer's hands
	/// </summary>
	public bool IsToHandSlot => fromSlot.ItemStorage.Player.OrNull()?.gameObject == Performer &&
	                              (targetSlot.SlotIdentifier.NamedSlot == NamedSlot.leftHand ||
	                               targetSlot.SlotIdentifier.NamedSlot == NamedSlot.rightHand);

	/// <summary>
	/// True if the alt button is pressed by the user. Performed clientside
	/// </summary>
	public bool IsAltClick;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">The gameobject of the player performing the InventoryApply</param>
	/// <param name="targetSlot">object that the player applying the used object to</param>
	/// <param name="fromSlot">hand slot if clicking on something in inventory, otherwise slot
	/// the item is being dragged from</param>
	private InventoryApply(GameObject performer, ItemSlot targetSlot, ItemSlot fromSlot, Intent intent, bool IsAltClick) :
		base(performer, fromSlot?.ItemObject, targetSlot?.ItemObject, intent)
	{
		this.fromSlot = fromSlot;
		this.targetSlot = targetSlot;
		this.IsAltClick = IsAltClick;
	}

	/// <summary>
	/// Create a InventoryApply interaction performed by the local player using their active hand
	/// </summary>
	/// <param name="targetObjectSlot">slot of the object that the player is applying the active hand item to</param>
	/// <param name="fromSlot">hand slot if clicking on something in inventory, otherwise slot
	/// the item is being dragged from</param>
	/// <returns></returns>
	public static InventoryApply ByLocalPlayer(ItemSlot targetObjectSlot, ItemSlot fromSlot)
	{
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			return Invalid;
		}
		return new InventoryApply(PlayerManager.LocalPlayer,
			targetObjectSlot, fromSlot, UIManager.CurrentIntent, KeyboardInputManager.IsAltPressed());
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
	/// <param name="fromSlot">hand slot if clicking on something in inventory, otherwise slot
	/// the item is being dragged from</param>
	/// <returns>a hand apply by the client, targeting the specified object with the item in the active hand</returns>

	public static InventoryApply ByClient(GameObject clientPlayer, ItemSlot targetObjectSlot,
		ItemSlot fromSlot, Intent intent, bool IsAltClick)
	{
		return new InventoryApply(clientPlayer, targetObjectSlot, fromSlot, intent, IsAltClick);
	}
}
