
/// <summary>
/// Client-side lifecycle hook for components. Invoked immediately before object will be despawned.
/// </summary>
public interface IOffStageClient
{
	/// <summary>
	/// Lifecycle hook invoked on client side immediately before object will despawn.
	/// Object should clear its state.
	/// </summary>
	void GoingOffStageClient();
}
