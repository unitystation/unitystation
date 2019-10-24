
/// <summary>
/// Represents a callback invoked when a progress action is done
/// </summary>
public interface IProgressEndAction
{
	/// <summary>
	/// Called when action progress ends
	/// </summary>
	/// <param name="reason">reason the progress ended</param>
	void OnEnd(ProgressEndReason reason);
}
