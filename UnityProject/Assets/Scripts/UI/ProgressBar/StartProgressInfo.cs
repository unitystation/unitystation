
using UnityEngine;

/// <summary>
/// Encapsulates the information related to an attempt to start a progress action
/// </summary>
public class StartProgressInfo
{
	/// <summary>
	/// How many seconds it should take to complete.
	/// </summary>
	public readonly float TimeForCompletion;

	/// <summary>
	/// Target of the progress.
	/// </summary>
	public readonly ActionTarget Target;

	/// <summary>
	/// GameObject of the player performing the action.
	/// </summary>
	public readonly GameObject Performer;

	/// <summary>
	/// Progress bar representing this action.
	/// </summary>
	public readonly ProgressBar ProgressBar;

	public StartProgressInfo(float timeForCompletion, ActionTarget target, GameObject performer,
		ProgressBar progressBar)
	{
		TimeForCompletion = timeForCompletion;
		Target = target;
		Performer = performer;
		ProgressBar = progressBar;
	}
}
