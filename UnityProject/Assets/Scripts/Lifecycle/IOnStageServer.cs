
/// <summary>
/// Server-side lifecycle hook for components. Invoked immediately after object has spawned + appeared.
/// </summary>
public interface IOnStageServer
{
	/// <summary>
	/// Lifecycle hook invoked on server side after object has spawned.
	/// Object should initialize its state.
	/// </summary>
	void GoingOnStageServer();
}
