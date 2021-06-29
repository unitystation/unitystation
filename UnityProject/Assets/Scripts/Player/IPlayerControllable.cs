/// <summary>
/// This interface is used with PlayerManager to pass
/// local movement actions onto an object such as the player prefab
/// Can be used to control anything via the movement keys
/// </summary>
public interface IPlayerControllable
{
	/// <summary>
	/// Received Player movement actions via PlayerManager when it is
	/// set to the local MovementControllable
	/// </summary>
	/// <param name="moveActions"></param>
	void ReceivePlayerMoveAction(PlayerAction moveActions);
}
