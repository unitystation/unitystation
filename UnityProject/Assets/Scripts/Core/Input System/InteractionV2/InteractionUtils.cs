
using System;
using System.Collections.Generic;
using System.Linq;
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
	/// component (or let the server determine the component if interactableComponent is null). The server will still validate the interaction and only perform
	/// it if it validates.
	///
	/// Note that if interactableComponent implements IClientSideInteractable for this interaction type,
	/// nothing will be done.
	/// </summary>
	/// <param name="interaction">interaction to perform</param>
	/// <param name="interactableComponent">component to handle the interaction, null
	/// if the server should determine which component will trigger from the interaction</param>
	/// <typeparam name="T">type of interaction</typeparam>
	public static void RequestInteract<T>(T interaction, IBaseInteractable<T> interactableComponent = null)
		where T : Interaction
	{
		if (!Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Interaction)) return;
		RequestInteractMessage.Send(interaction, interactableComponent);
	}


	/// <summary>
	/// Checks if this component would trigger and sends the interaction request to the server if it does. Doesn't
	/// send a message if it only triggered an IClientInteractable (client side only) interaction.
	/// </summary>
	/// <param name="interactable">component to check / trigger</param>
	/// <param name="interaction">interaction attempting to be performed</param>
	/// <typeparam name="T">interaction type</typeparam>
	/// <returns>true if an interaction was triggered (even if it was clientside-only)</returns>
	public static bool ClientCheckAndTrigger<T>(this IBaseInteractable<T> interactable, T interaction) where T: Interaction
	{
		if (CheckInteractInternal(interactable, interaction, NetworkSide.Client, out var wasClientInteractable))
		{
			//if it was a client-side only interaction, we don't send an interaction request
			if (!wasClientInteractable)
			{
				// we defer to the server for deciding what interaction was triggered, unless this is
				// an AimApply in which case there's no reason for such a thing (no shooting-like interactions
				// should have multiple interactable AimApply components)
				RequestInteract(interaction, typeof(T) == typeof(AimApply) ? interactable : null);
			}

			return true;
		}

		return false;
	}

	/// <summary>
	/// Client side interaction checking logic. Goes through each interactable component (starting from the bottom of the object's
	/// component list) and checks for triggered interactions, returning the first one that
	/// triggers an interaction, null if none are triggered. Messages the server to request an interaction for the interaction that
	/// was triggered (if any). Doesn't message the server if only a clientside interaction was triggered.
	/// </summary>
	/// <param name="interactables">component to check</param>
	/// <param name="interaction">interaction attempting to be performed</param>
	/// <typeparam name="T">type of interaction being checked</typeparam>
	/// <returns></returns>
	public static IBaseInteractable<T> ClientCheckAndTrigger<T>(IEnumerable<IBaseInteractable<T>> interactables, T interaction)
		where T : Interaction
	{
		if (Cooldowns.IsOnClient(interaction, CommonCooldowns.Instance.Interaction)) return null;
		Logger.LogTraceFormat("Checking {0} interactions",
			Category.Interaction, typeof(T).Name);
		foreach (var interactable in interactables.Reverse())
		{
			if (ClientCheckAndTrigger(interactable, interaction))
			{
				return interactable;
			}
		}

		return null;
	}

	private static bool CheckInteractInternal<T>(this IBaseInteractable<T> interactable, T interaction,
		NetworkSide side, out bool wasClientInteractable)
		where T : Interaction
	{
		wasClientInteractable = false;
		//interactions targeting an object at hiddenpos are NEVER allowed (except for inventory actions,
		//since they can target an object in inventory which means its at hiddenpos)
		if (!(interaction is InventoryApply) && interaction is TargetedInteraction targetedInteraction)
		{
			if (targetedInteraction.TargetObject != null &&
			    targetedInteraction.TargetObject.IsAtHiddenPos())
			{
				Logger.LogTraceFormat("Aborting {0} interaction on object {1} because the object is hidden.",
					Category.Interaction, typeof(T).Name, targetedInteraction.TargetObject.name);
				return false;
			}
		}
		if (Cooldowns.IsOn(interaction, CooldownID.Asset(CommonCooldowns.Instance.Interaction, side))) return false;
		var result = false;
		//check if client side interaction should be triggered
		if (side == NetworkSide.Client && interactable is IClientInteractable<T> clientInteractable)
		{
			result = clientInteractable.Interact(interaction);
			if (result)
			{
				Logger.LogTraceFormat("ClientInteractable triggered from {0} on {1} for object {2}", Category.Interaction, typeof(T).Name, clientInteractable.GetType().Name,
					(clientInteractable as Component).gameObject.name);
				Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Interaction);
				wasClientInteractable = true;
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
				wasClientInteractable = false;
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
				wasClientInteractable = false;
				return true;
			}
		}

		Logger.LogTraceFormat("No interaction triggered from {0} on {1} for object {2}", Category.Interaction, typeof(T).Name, interactable.GetType().Name,
			(interactable as Component).gameObject.name);

		wasClientInteractable = false;
		return false;
	}


	/// <summary>
	/// Checks if this component should trigger based on server-side logic.
	/// </summary>
	/// <param name="interactable">component to check</param>
	/// <param name="interaction">interaction attempting to be performed</param>
	/// <typeparam name="T">type of interaction</typeparam>
	/// <returns>true if an interaction would be triggered.</returns>
	public static bool ServerCheckInteract<T>(this IBaseInteractable<T> interactable, T interaction)
		where T : Interaction
	{
		return CheckInteractInternal(interactable, interaction, NetworkSide.Server, out var unused);
	}
}
