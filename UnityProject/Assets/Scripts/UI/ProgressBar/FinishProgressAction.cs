using System;

/// <summary>
/// Defines what to do when finishing progress bar, which could be due to the progress completing
/// or being interrupted. Pretty sure this runs only on the server.
/// </summary>
public partial class FinishProgressAction
{
	//callback invoked when action completes
	private Action<FinishReason> onFinished;

	/// <summary>
	/// Finish progress action with a specified callback when finished
	/// </summary>
	/// <param name="onFinished">function to invoke when progress is finished, including an indicator
	/// of why the progress finished (such as if it was interrupted). The function should
	/// take care of whatever needs to be done based on FinishStatus status.</param>
	public FinishProgressAction(Action<FinishReason> onFinished)
	{
		this.onFinished = onFinished;
	}

	/// <summary>
	/// Finish the action with the specified reason, invoke the callback.
	/// </summary>
	/// <param name="completed">reason for completion</param>
	public void Finish(FinishReason reason)
	{
		this.onFinished.Invoke(reason);
	}
}