
/// <summary>
/// Indicates a component which can process a particular kind of interaction when it is
/// involved in the interaction.
/// </summary>
/// <typeparamref name="T">Interaction subtype
/// for the interaction that this component wants to handle (such as MouseDrop for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
public interface IInteractable<T>
	where T : Interaction
{

	/// <summary>
	/// Handle the interaction
	/// </summary>
	/// <param name="interaction"></param>
	/// <returns>true indicates that this component "consumed" the event -
	/// no further processing should happen for this interaction - no other components
	/// should process the event. Typically returned when the interaction has caused something to occur.
	///
	/// False indicates the component didn't consume the event - other components should be allowed to
	/// process the event. Typically returned when
	/// the interaction didn't do anything, due to being invalid or inconsequential.
	/// </returns>
	bool Interact(T interaction);
}
