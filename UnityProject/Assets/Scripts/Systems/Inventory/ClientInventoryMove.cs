
/// <summary>
/// Provides details about the inventory movement that the server told us about.
/// </summary>
public class ClientInventoryMove
{
	/// <summary>
	/// Type of move that occurred
	/// </summary>
	public readonly ClientInventoryMoveType ClientInventoryMoveType;

	private ClientInventoryMove(ClientInventoryMoveType clientInventoryMoveType)
	{
		ClientInventoryMoveType = clientInventoryMoveType;
	}

	/// <summary>
	/// An inventory move of the indicated type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static ClientInventoryMove OfType(ClientInventoryMoveType type)
	{
		return new ClientInventoryMove(type);
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