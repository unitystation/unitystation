
/// <summary>
/// Server-side lifecycle hook for components. Invoked immediately after object has spawned + appeared.
/// </summary>
public interface IServerSpawn
{
	/// <summary>
	/// Lifecycle hook invoked on server side after object has spawned.
	/// Object should initialize its state.
	/// </summary>
	/// <param name="info">info about how it is being spawned</param>
	void OnSpawnServer(SpawnInfo info);
}
