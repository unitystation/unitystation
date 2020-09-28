
/// <summary>
/// Defines a client-side-only interaction. No IF2 networking logic will be called for this interaction type
/// on this component, it's up to the implementation of this interface to call any necessary networking.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IClientInteractable<T> : IBaseInteractable<T>
	where T : Interaction
{
	/// <summary>
	/// Run the client-side interaction logic
	/// </summary>
	/// <param name="interaction"></param>
	/// <returns>true if further interaction checking should stop (typically you would return true if something
	/// actually happened).</returns>
	bool Interact(T interaction);
}
