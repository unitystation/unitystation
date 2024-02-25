
/// <summary>
/// Implement this on a component to fire logic when matrix rotation starts, ends, or the object
/// is being newly registered as being on a particular matrix. This is invoked
/// on the server and client (and both when it's server player's game)
/// </summary>
public interface IMatrixRotation
{
	/// <summary>
	/// Invoked when matrix rotation starts, ends, or the object is being newly registered
	/// as being on a particular metrix.
	/// Called on client and server side (and both when it's
	/// server player's game).
	/// Use rotationInfo.NetworkSide to distinguish between client / server logic.
	/// Can use rotationInfo.IsStart, IsEnd, IsObjectBeingRegistered to distinguish between different kinds of
	/// rotation events.
	/// </summary>
	void OnMatrixRotate();
}
