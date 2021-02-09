
/// <summary>
/// Server-side lifecycle hook for components. Invoked immediately before object will be despawned.
/// </summary>
public interface IServerDespawn
{
	/// <summary>
	/// Lifecycle hook invoked on server side immediately before object will despawn.
	/// Object should clear its state.
	/// </summary>
	/// <param name="info">info about how it is being despawned</param>
	void OnDespawnServer(DespawnInfo info);
}
