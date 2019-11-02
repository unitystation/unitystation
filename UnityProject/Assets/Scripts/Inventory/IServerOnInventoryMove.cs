
/// <summary>
/// Server side only. Add this to a component to allow it to invoke some logic
/// when its object is moved within, into, or out of the inventory system.
/// </summary>
public interface IServerOnInventoryMove
{
	/// <summary>
	/// Invoked when the object is moved within, into, or out of the inventory system.
	/// </summary>
	/// <param name="info"></param>
	void ServerOnInventoryMove(InventoryMove info);
}
