
/// <summary>
/// Server-side lifecycle hook for components. Invoked immediately before object will be despawned.
///
/// NOTE: No client side off-stage hook because we wouldn't be able
/// to ensure that the hook is called before Unet destroys the
/// client-side object unless we implement a custom destruction
/// syncing logic.
/// </summary>
public interface IOffStageServer
{
	/// <summary>
	/// Lifecycle hook invoked on server side immediately before object will despawn.
	/// Object should clear its state.
	/// </summary>
	/// <param name="info">info about how it is being despawned</param>
	void GoingOffStageServer(OffStageInfo info);
}
