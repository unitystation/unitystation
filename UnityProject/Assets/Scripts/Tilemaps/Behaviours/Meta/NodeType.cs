public enum NodeType
{
	None,
	/// <summary>
	/// Node out in space
	/// </summary>
	Space,
	/// <summary>
	/// Node in a room on a tile that is not occupied.
	/// </summary>
	Room,
	/// <summary>
	/// Node occupied by something such that it is not passable.
	/// </summary>
	Occupied
}