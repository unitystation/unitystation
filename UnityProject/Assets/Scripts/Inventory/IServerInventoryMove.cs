
/// <summary>
/// Server side only. Add this to a component to allow it to invoke some logic
/// when its object is moved within, into, or out of the inventory system.
/// </summary>
public interface IServerInventoryMove
{
	/// <summary>
	/// Invoked when the object is moved within, into, or out of the inventory system on server side.
	/// </summary>
	/// <param name="info"></param>
	void OnInventoryMoveServer(InventoryMove info);
}
