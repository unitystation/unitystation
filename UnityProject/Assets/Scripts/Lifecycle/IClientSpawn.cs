
/// <summary>
/// Client-side lifecycle hook for components.
/// Invoked immediately after object has spawned + appeared or when object
/// is mapped in scene and scene has loaded, or when client has just joined and the
/// objects have been synced.
/// </summary>
public interface IClientSpawn
{
	/// <summary>
	/// Lifecycle hook invoked on client side after object has spawned, or when object
	/// is mapped in scene and scene has loaded, or when client has just joined and
	/// the objects have been synced.
	/// </summary>
	/// <param name="info">info about how it is being spawned</param>
	void OnSpawnClient(ClientSpawnInfo info);
}
