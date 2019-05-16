
/// <summary>
/// Communicates the result of attempting to perform some sort of interaction.
/// Each interaction result has an indication of whether
/// something happened or didn't.
///
/// Currently that's all it does, but it can be expanded if more info is needed
/// to support coordination between various interactions that can occur in a given situation.
/// </summary>
public class InteractionResult
{
	/// <summary>
	/// Indicates something happened which caused some consequence.
	/// </summary>
	public static readonly InteractionResult SOMETHING_HAPPENED
		= new InteractionResult(true);

	/// <summary>
	/// Indicates nothing happened at all of any consequence.
	/// </summary>
	public static readonly InteractionResult NOTHING_HAPPENED
		= new InteractionResult(false);

	private bool somethingHappened;
	/// <summary>
	/// True iff some interaction occurred / something consequential happened
	/// </summary>
	public bool SomethingHappened => somethingHappened;

	private InteractionResult(bool somethingHappened)
	{
		this.somethingHappened = somethingHappened;
	}
}
