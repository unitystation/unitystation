
/// <summary>
/// When an interaction event occurs (such as clicking the game world), there
/// may be multiple components on multiple objects that could do something in response to the
/// event. This class allows each component to communicate what should be done next after it has had a chance
/// to handle the event.
/// </summary>
public class InteractionControl
{
	/// <summary>
	/// Indicates no further processing should happen for this interaction - no other components
	/// should process the event. Typically returned when the interaction has caused something to occur.
	/// </summary>
	public static readonly InteractionControl STOP_PROCESSING
		= new InteractionControl(true);

	/// <summary>
	/// Indicates other components should be allowed to process the event. Typically returned when
	/// the interaction didn't do anything, due to being invalid or inconsequential.
	/// </summary>
	public static readonly InteractionControl CONTINUE_PROCESSING
		= new InteractionControl(false);

	private bool stopProcessing;
	/// <summary>
	/// True iff some interaction occurred / something consequential happened
	/// </summary>
	public bool StopProcessing => stopProcessing;

	private InteractionControl(bool stopProcessing)
	{
		this.stopProcessing = stopProcessing;
	}
}
