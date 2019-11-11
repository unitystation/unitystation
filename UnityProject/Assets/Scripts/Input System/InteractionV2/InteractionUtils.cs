
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utilities to be used by interactable components and other IF2 classes.
/// </summary>
public static class InteractionUtils
{

	/// <summary>
	/// Use this on client side to request an interaction. This can be used to trigger
	/// interactions manually outside of the normal interaction logic.
	/// Request the server to perform the indicated interaction using the specified
	/// component. The server will still validate the interaction and only perform
	/// it if it validates.
	///
	/// Note that if interactableComponent implements IClientSideInteractable for this interaction type,
	/// nothing will be done.
	/// </summary>
	/// <param name="interaction">interaction to perform</param>
	/// <param name="interactableComponent">component to handle the interaction</param>
	/// <typeparam name="T"></typeparam>
	public static void RequestInteract<T>(T interaction, IBaseInteractable<T> interactableComponent)
		where T : Interaction
	{
		RequestInteractMessage.Send(interaction, interactableComponent);
	}

	/// <summary>
	/// Client side interaction checking logic. Goes through each interactable component and checks for triggered interactions, returning the first one that
	/// triggers an interaction, null if none are triggered. Messages the server to request an interaction for the interaction that
	/// was triggered (if any).
	/// </summary>
	/// <param name="interactables"></param>
	/// <param name="interaction"></param>
	/// <param name="side"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static IBaseInteractable<T> ClientCheckAndTrigger<T>(IEnumerable<IBaseInteractable<T>> interactables, T interaction)
		where T : Interaction
	{
		Logger.LogTraceFormat("Checking {0} interactions",
			Category.Interaction, typeof(T).Name);
		foreach (var interactable in interactables)
		{
			if (interactable.CheckInteract(interaction, NetworkSide.Client))
			{
				RequestInteract(interaction, interactable);
				return interactable;
			}
		}

		return null;
	}

	/// <summary>
	/// Checks if this component would trigger any interaction, also invokes client side logic if it implements IClientInteractable.
	/// </summary>
	/// <param name="interactable"></param>
	/// <param name="interaction"></param>
	/// <param name="side"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static bool CheckInteract<T>(this IBaseInteractable<T> interactable, T interaction, NetworkSide side)
		where T : Interaction
	{
		var result = false;
		//check if client side interaction should be triggered
		if (side == NetworkSide.Client && interactable is IClientInteractable<T> clientInteractable)
		{
			result = clientInteractable.Interact(interaction);
			if (result)
			{
				Logger.LogTraceFormat("ClientInteractable triggered from {0} on {1} for object {2}", Category.Interaction, typeof(T).Name, clientInteractable.GetType().Name,
					(clientInteractable as Component).gameObject.name);
				return true;
			}
		}
		//check other kinds of interactions
		if (interactable is ICheckable<T> checkable)
		{
			result = checkable.WillInteract(interaction, side);
			if (result)
			{
				Logger.LogTraceFormat("WillInteract triggered from {0} on {1} for object {2}", Category.Interaction, typeof(T).Name, checkable.GetType().Name,
					(checkable as Component).gameObject.name);
				return true;
			}
		}
		else if (interactable is IInteractable<T>)
		{
			//use default logic
			result = DefaultWillInteract.Default(interaction, side);
			if (result)
			{
				Logger.LogTraceFormat("WillInteract triggered from {0} on {1} for object {2}", Category.Interaction, typeof(T).Name, interactable.GetType().Name,
					(interactable as Component).gameObject.name);
				return true;
			}
		}

		Logger.LogTraceFormat("No interaction triggered from {0} on {1} for object {2}", Category.Interaction, typeof(T).Name, interactable.GetType().Name,
			(interactable as Component).gameObject.name);

			return false;
	}
}
