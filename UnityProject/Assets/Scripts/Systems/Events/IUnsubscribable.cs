
/// <summary>
/// Indicates a class which knows how to unsubscribe itself from an event.
/// </summary>
public interface IUnsubscribable
{
	/// <summary>
	/// The action should unsubscribe itself to whatever it was subscribed to.
	/// </summary>
	void Unsubscribe();
}
