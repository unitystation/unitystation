
/// <summary>
/// Indicates an Interactable Component which can check whether an interaction will occur with it.
/// If your component has an interaction but doesn't implement this, it will
/// use the default validations defined in DefaultWillInteract.
///
/// By implementing this and adding more specific logic, you can reduce the amount of messages
/// sent by the client to the server, decreasing overall network load.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ICheckable<T>
	where T : Interaction
{
	/// <summary>
	/// Decides if interaction logic should proceed. On client side, the interaction
	/// request will only be sent to the server if this returns true. On server side,
	/// the interaction will only be performed if this returns true.
	/// </summary>
	/// <param name="interaction">interaction to validate</param>
	/// <param name="side">which side of the network this is being invoked on</param>
	/// <returns>True/False based on whether the interaction logic should proceed as described above.</returns>
	bool WillInteract(T interaction, NetworkSide side);
}
