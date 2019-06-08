
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
	/// <returns>what should happen next for this interaction event</returns>
	InteractionControl Interact(T interaction);
}
