/// <summary>
/// Defines a particular way of determining if a player can reach something.
/// </summary>
public enum ReachRange
{

	/// <summary>
	///based on standard interaction distance (playerScript.interactionDistance)
	/// </summary>
	Standard,
	//object can still be in reach even if outside standard interactionDistance - such as for an object not
	//perfectly aligned on the tile. In either case, range will not be checked on the client side - it will only
	//be checked on server side
	/// <summary>
	/// object can still be in reach even if outside standard interactionDistance - such as for an object not
	/// perfectly aligned on the tile. In either case, range will not be checked on the client side - it will only
	/// be checked on server side
	/// </summary>
	ExtendedServer,
	/// <summary>
	/// Don't check range at all.
	/// </summary>
	Unlimited,
	/// <summary>
	/// FOV Distance
	/// </summary>
	Telekinesis
}