
/// <summary>
/// Encapsulates the information related to an action in progress.
/// Struct to avoid creating lots of garbage, since this is created each tick of progress.
/// </summary>
public struct InProgressInfo
{
	/// <summary>
	/// How much time (in seconds) has transpired since progress started.
	/// </summary>
	public readonly float ProgressTime;

	public InProgressInfo(float progressTime)
	{
		ProgressTime = progressTime;
	}
}
