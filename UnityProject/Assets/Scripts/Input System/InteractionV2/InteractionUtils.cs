
using System;
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

	public static bool CheckInteract<T>(this IBaseInteractable<T> interactable, T interaction, NetworkSide side)
		where T : Interaction
	{
		//check if client side interaction should be triggered
		if (side == NetworkSide.Client && interactable.GetType().IsAssignableFrom(typeof(IClientInteractable<T>)))
		{
			return (interactable as IClientInteractable<T>).Interact(interaction);
		}
		else if (interactable.GetType().IsAssignableFrom(typeof(ICheckable<T>)))
		{
			return (interactable as ICheckable<T>).WillInteract(interaction, side);
		}
		else if (interactable.GetType().IsAssignableFrom(typeof(IInteractable<T>)))
		{
			//use default logic
			return DefaultWillInteract.Default(interaction, side);
		}

		return false;
	}
}
