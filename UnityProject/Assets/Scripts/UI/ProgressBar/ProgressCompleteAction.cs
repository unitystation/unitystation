
using System;

/// <summary>
/// IProgressEndAction which only invokes the callback when action is done due to being completed (since most
/// actions in the game don't care to do anything when it's interrupted. Does nothing
/// when action is interrupted.
/// </summary>
public class ProgressCompleteAction : IProgressEndAction
{
	//callback invoked when action completes
	private Action onComplete;

	/// <summary>
	///
	/// </summary>
	/// <param name="onComplete">to call when action is done due to being completed.</param>
	public ProgressCompleteAction(Action onComplete)
	{
		this.onComplete = onComplete;
	}

	public void OnEnd(ProgressEndReason reason)
	{
		if (reason == ProgressEndReason.COMPLETED)
		{
			onComplete.Invoke();
		}
	}
}
