
/// <summary>
/// Indicates an Interactable Component, which has some server-side interaction logic.
/// </summary>
public interface IInteractable<T> : IBaseInteractable<T>
	where T : Interaction
{
	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	void ServerPerformInteraction(T interaction);

}
