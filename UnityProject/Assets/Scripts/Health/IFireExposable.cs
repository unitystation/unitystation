/// <summary>
/// Implement this on a component to cause that component to do something when in contact with fire. It's up to the component what
/// it does in response to fire exposure.
/// </summary>
public interface IFireExposable
{

	/// <summary>
	/// Expose this component to fire.
	/// </summary>
	/// <param name="exposure">information on the exposure</param>
	void OnExposed(FireExposure exposure);
}