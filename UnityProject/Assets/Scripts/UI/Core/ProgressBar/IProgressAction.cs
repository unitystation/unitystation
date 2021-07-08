
/// <summary>
/// Class which can function as a progress action - an action that takes some amount of time and can
/// be interrupted due to various factors.
/// </summary>
public interface IProgressAction
{
	/// <summary>
	/// Starts the progress action, any startup logic can be placed here. Return
	/// false to indicate that progress should not be started and this attempt to start
	/// should be canceled
	/// </summary>
	/// <param name="info"></param>
	/// <returns>true iff progress should be started. false if not.</returns>
	bool OnServerStartProgress(StartProgressInfo info);

	/// <summary>
	/// Invoked repeatedly as progress is made. Return false to prematurely interrupt progress.
	/// </summary>
	/// <param name="info"></param>
	/// <returns></returns>
	bool OnServerContinueProgress(InProgressInfo info);

	/// <summary>
	/// Invoked when progress ends for some reason, which may be due to successful completion
	/// or interruption.
	/// </summary>
	/// <param name="info"></param>
	void OnServerEndProgress(EndProgressInfo info);
}
