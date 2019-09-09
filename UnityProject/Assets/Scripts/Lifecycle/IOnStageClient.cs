
/// <summary>
/// Client-side lifecycle hook for components. Invoked immediately after object has spawned + appeared.
/// </summary>
public interface IOnStageClient
{
	/// <summary>
	/// Lifecycle hook invoked on client side after object has spawned.
	/// Object should initialize its state.
	/// </summary>
	/// <param name="info">info about how it is being spawned</param>
	void GoingOnStageClient(OnStageInfo info);
}
