
/// <summary>
/// Server-side lifecycle hook for components.
/// Invoked immediately after object has spawned + appeared, or when object
/// is mapped in the scene and scene has loaded.
/// </summary>
public interface IServerSpawn
{
	/// <summary>
	/// Lifecycle hook invoked on server side after object has spawned or when object
	/// is mapped in the scene and scene has loaded.
	/// Object should initialize its state.
	/// </summary>
	/// <param name="info">info about how it is being spawned</param>
	void OnSpawnServer(SpawnInfo info);
}
