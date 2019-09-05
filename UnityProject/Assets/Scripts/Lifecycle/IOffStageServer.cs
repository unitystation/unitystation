
/// <summary>
/// Server-side lifecycle hook for components. Invoked immediately before object will be despawned.
/// </summary>
public interface IOffStageServer
{
	/// <summary>
	/// Lifecycle hook invoked on server side immediately before object will despawn.
	/// Object should clear its state.
	/// </summary>
	void GoingOffStageServer();
}
