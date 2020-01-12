
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
		if (!PlayerManager.LocalPlayerScript.TryStartCooldown(CooldownType.Interaction, true)) return;
		RequestInteractMessage.Send(interaction, interactableComponent);
	}

	/// <summary>
	/// Client side interaction checking logic. Goes through each interactable component (starting from the bottom of the object's
	/// component list) and checks for triggered interactions, returning the first one that
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
		if (interaction.PerformerPlayerScript.IsOnCooldown(CooldownType.Interaction)) return null;
		Logger.LogTraceFormat("Checking {0} interactions",
			Category.Interaction, typeof(T).Name);
		foreach (var interactable in interactables.Reverse())
		{
			if (interactable.CheckInteractInternal(interaction, NetworkSide.Client, out var wasClientInteractable))
			{
				//don't send a message for clientside-only interactions
				if (!wasClientInteractable)
				{
					RequestInteract(interaction, interactable);
				}
				return interactable;
			}
		}

		return null;
	}

	private static bool CheckInteractInternal<T>(this IBaseInteractable<T> interactable, T interaction,
		NetworkSide side, out bool wasClientInteractable)
		where T : Interaction
	{
		if (interaction.PerformerPlayerScript.IsOnCooldown(CooldownType.Interaction)) return false;
		var result = false;
		//check if client side interaction should be triggered
		if (side == NetworkSide.Client && interactable is IClientInteractable<T> clientInteractable)
		{
			result = clientInteractable.Interact(interaction);
			if (result)
			{
				Logger.LogTraceFormat("ClientInteractable triggered from {0} on {1} for object {2}", Category.Interaction, typeof(T).Name, clientInteractable.GetType().Name,
					(clientInteractable as Component).gameObject.name);
				//we don't start this as the host player because some clientside interactions (like hugging) trigger
				//server side logic through Cmds or net messages which also check the cooldowns, thus host player
				//would always prevent themselves from performing these interactions
				interaction.PerformerPlayerScript.TryStartCooldown(CooldownType.Interaction, true);
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
		return CheckInteractInternal(interactable, interaction, side, out var unused);
	}
}
