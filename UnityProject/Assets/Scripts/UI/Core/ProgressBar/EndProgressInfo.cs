
/// <summary>
/// Encapsulates info on why progress action is ending.
/// </summary>
public class EndProgressInfo
{
	/// <summary>
	/// Was progress completed successfully?
	/// </summary>
	public readonly bool WasCompleted;

	/// <summary>
	/// Was progress prematurely interrupted?
	/// </summary>
	public bool WasInterrupted => !WasCompleted;

	public EndProgressInfo(bool wasCompleted)
	{
		WasCompleted = wasCompleted;
	}
}
