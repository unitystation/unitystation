
/// <summary>
/// Indicates a component which can process a particular kind of interaction when it is
/// involved in the interaction.
/// </summary>
/// <typeparamref name="T">Interaction subtype
/// for the interaction that this component wants to handle (such as MouseDropInfo for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
public interface IInteractable<T>
	where T : Interaction
{

	InteractionResult Interact(T interaction);
}
