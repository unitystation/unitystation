
/// <summary>
/// Client side inventory movement. Add this to a component to allow it to invoke some logic
/// when the server tells us its object is moved within, into, or out of the inventory system.
/// </summary>
public interface IClientInventoryMove
{
	/// <summary>
	/// Invoked when server tells us that the object is moved within, into, or out of the inventory system.
	/// Invoked after the movement has already occurred.
	/// </summary>
	/// <param name="info"></param>
	void OnInventoryMoveClient(ClientInventoryMove info);
}
