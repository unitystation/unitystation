
/// <summary>
/// Should go on a component. Allows component to define the possible actions
/// that may be performed. Represents a component which can perform actions in response to
/// a given type of interaction.
///
/// This may also be useful to eventually allow actions to be pre-empted by other actions, by letting
/// components look at what actions are currently possible or indicating certain actions as blacklisted.
/// </summary>
/// <typeparam name="T">type of interaction this component will respond to</typeparam>
public interface IActionable<T>
	where T : Interaction
{

	/// <summary>
	/// Register this components possible actions by adding them to registered.
	/// </summary>
	/// <param name="registered">currently registered actions. use registered.Register to
	/// register additional actions.</param>
	void RegisterActions(RegisteredActions registered);
}
