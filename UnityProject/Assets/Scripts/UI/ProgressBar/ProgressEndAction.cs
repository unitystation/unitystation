using System;

/// <summary>
/// Defines what to do when finishing progress bar, which could be due to the progress completing
/// or being interrupted. Only runs on the server.
/// </summary>
public class ProgressEndAction : IProgressEndAction
{
	//callback invoked when action finishes
	private Action<ProgressEndReason> onEnd;

	/// <summary>
	/// End progress action with a specified callback when finished
	/// </summary>
	/// <param name="onEnd">function to invoke when progress is ended, including an indicator
	/// of why the progress ended (such as if it was interrupted). The function should
	/// take care of whatever needs to be done based on ProgressEndReason status.</param>
	public ProgressEndAction(Action<ProgressEndReason> onEnd)
	{
		this.onEnd = onEnd;
	}

	/// <summary>
	/// Finish the action with the specified reason, invoke the callback.
	/// </summary>
	/// <param name="completed">reason for completion</param>
	public void OnEnd(ProgressEndReason reason)
	{
		this.onEnd.Invoke(reason);
	}
}