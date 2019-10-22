
/// <summary>
/// Indicates a component which can process a RequestInteractMessage on the server.
///
/// Must be implemented on a Component.
/// </summary>
/// <typeparamref name="T">Interaction subtype
/// for the interaction that this component wants to handle (such as MouseDrop for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
public interface IInteractionProcessor<T>
	where T : Interaction
{
	/// <summary>
	/// Server-side only. Invoked when server receives a RequestInteractMessage
	/// which indicates this component should perform the processing.
	/// Validate the interaction and determine the new state
	/// of the game world if validation succeeds. Then inform whatever clients need to be informed.
	/// </summary>
	/// <param name="interaction">interaction being performed </param>
	/// <returns>what should happen next for this interaction attempt</returns>
	bool ServerProcessInteraction(T interaction);
}
