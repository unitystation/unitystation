/// <summary>
/// This is used to represent the status of a player's Mind object when it comes to
/// cloning. The only item here that allows for cloning is Cloneable, the others are
/// just there to inform why the player can't be cloned.
/// </summary>
public enum CloneableStatus
{
	Cloneable,
	OldRecord,
	DenyingCloning,
	StillAlive,
	Offline
}
