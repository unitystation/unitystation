
using System;
using System.Collections.Generic;

/// <summary>
/// Tracks which actions are currently possible for a given interaction.
/// </summary>
public class RegisteredActions
{
	private List<UAction> registeredActions = new List<UAction>();

	/// <summary>
	/// Register these as currently possible actions.
	/// </summary>
	/// <param name="toRegister"></param>
	public void Register(params UAction[] toRegister)
	{
		registeredActions.AddRange(toRegister);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="actionType"></param>
	/// <returns>UAction of the specified type that has been registered, null if none found</returns>
	public UAction GetAction(Type actionType)
	{
		return registeredActions.Find(action => actionType == action.GetType());
	}
}
