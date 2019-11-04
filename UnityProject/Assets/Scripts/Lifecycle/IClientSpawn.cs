
/// <summary>
/// Client-side lifecycle hook for components.
/// Invoked immediately after object has spawned + appeared or when object
/// is mapped in scene and scene has loaded.
/// </summary>
public interface IClientSpawn
{
	/// <summary>
	/// Lifecycle hook invoked on client side after object has spawned or when object
	/// is mapped in scene and scene has loaded.
	/// Object should initialize its state.
	/// </summary>
	/// <param name="info">info about how it is being spawned</param>
	void OnSpawnClient(ClientSpawnInfo info);
}
