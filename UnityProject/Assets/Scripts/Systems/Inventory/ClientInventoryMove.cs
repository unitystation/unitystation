
/// <summary>
/// Provides details about the inventory movement that the server told us about.
/// </summary>
public class ClientInventoryMove
{
	/// <summary>
	/// Type of move that occurred
	/// </summary>
	public readonly ClientInventoryMoveType ClientInventoryMoveType;

	/// <summary>
	/// Slot the MovedObject was moved from (null if InventoryMove.Add)
	/// </summary>
    public readonly ItemSlot FromSlot;

	/// <summary>
	/// Slot the MovedObject was moved to (null if InventoryMove.Remove)
	/// </summary>
    public readonly ItemSlot ToSlot;

	private ClientInventoryMove(ClientInventoryMoveType clientInventoryMoveType, ItemSlot fromSlot, ItemSlot toSlot)
    {
        ClientInventoryMoveType = clientInventoryMoveType;
    }

    /// <summary>
    /// An inventory move of the indicated type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
	public static ClientInventoryMove OfType(ClientInventoryMoveType type, ItemSlot fromSlot, ItemSlot toSlot)
    {
        return new ClientInventoryMove(type, fromSlot, toSlot);
    }

}

public enum ClientInventoryMoveType
{
	/// <summary>
	/// Item was removed from a slot
	/// </summary>
	Removed,
	/// <summary>
	/// Item was added to a slot
	/// </summary>
	Added
}