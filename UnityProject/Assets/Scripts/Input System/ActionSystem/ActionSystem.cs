using System;
using UnityEngine;

/// <summary>
/// Main API for action system
/// </summary>
public static class ActionSystem
{
	/// <summary>
	/// Gets the UAction (if one exists) on the specified onObject for the specified component
	/// of the specified action type.
	/// </summary>
	/// <param name="interaction">interaction we are finding actions for</param>
	/// <param name="onObject">object to look on</param>
	/// <param name="componentType">component (which implements IActionable) to check on the object</param>
	/// <param name="actionType">type of action we are looking for</param>
	/// <returns>the UAction of the specified type, null if not found</returns>
	private static UAction FindActionInternal<T>(T interaction, GameObject onObject, Type componentType, Type actionType)
		where T : Interaction
	{
		var component = onObject.GetComponent(componentType);
		if (component == null || !typeof(Component).IsAssignableFrom(typeof(IActionable<T>))) return null;

		var actionable = component as IActionable<T>;
		var registered = new RegisteredActions();
		actionable.RegisterActions(registered);

		return registered.GetAction(actionType);
	}

	public static UAction FindAction(Interaction interaction, GameObject onObject, Type componentType, Type actionType)
	{
		if (interaction.GetType() is PositionalHandApply)
		{
			return FindActionInternal(interaction as PositionalHandApply, onObject, componentType, actionType);
		}
		else if (interaction.GetType() is HandApply)
		{
			return FindActionInternal(interaction as HandApply, onObject, componentType, actionType);
		}
		else if (interaction is AimApply)
		{
			return FindActionInternal(interaction as AimApply, onObject, componentType, actionType);
		}
		else if (interaction is MouseDrop)
		{
			return FindActionInternal(interaction as MouseDrop, onObject, componentType, actionType);
		}
		else if (interaction is HandActivate)
		{
			return FindActionInternal(interaction as HandActivate, onObject, componentType, actionType);
		}
		else if (interaction.GetType() is InventoryApply)
		{
			return FindActionInternal(interaction as InventoryApply, onObject, componentType, actionType);
		}

		return null;
	}
}
