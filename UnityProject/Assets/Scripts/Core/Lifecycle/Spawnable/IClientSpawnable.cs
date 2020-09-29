
/// <summary>
/// Indicates a class which can spawn something at a given destination on the
/// client side. For client-side only stuff (like bullets).
/// </summary>
public interface IClientSpawnable
{

	/// <summary>
	/// Spawn something at the indicated destination on the client side.
	/// </summary>
	/// <param name="destination">description of where it should be spawned.</param>
	/// <returns>result of attempting to spawn</returns>
	SpawnableResult ClientSpawnAt(SpawnDestination destination);
}
