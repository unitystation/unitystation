
/// <summary>
/// Indicates a class which can be used to spawn something at a given destination on the server.
/// </summary>
public interface ISpawnable
{
	/// <summary>
	/// Spawn something at the indicated destination on the server
	/// </summary>
	/// <param name="destination">description of where it should be spawned.</param>
	/// <returns>result of attempting to spawn</returns>
	SpawnableResult SpawnAt(SpawnDestination destination);
}
