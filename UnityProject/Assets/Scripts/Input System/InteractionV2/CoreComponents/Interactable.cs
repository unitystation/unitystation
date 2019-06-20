
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When implementing a IInteractable component, you can extend this if your use case
/// matches this flow (which most of them do), but don't forget to call
/// base.Start if you declare Start:
///
/// 1. Client validation logic.
/// 2. If validation succeeds, sends RequestInteractMessage to server telling it to perform the interaction.
/// 3. Server validates the requested interaction.
/// 4. If validation succeeds, server performs the interaction and informs clients as needed.
///
/// If you don't want to subclass this, you can instead use InteractionCoordinator in your component
/// and delegate to it,
/// but this class provides an alternative with slightly less boilerplate.
///
/// Subclasses simply need to implement the validation logic and the server-side interaction logic, and
/// this class care of the rest.
/// </summary>
/// <typeparamref name="T">Interaction subtype
/// for the interaction that this component wants to handle (such as MouseDrop for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
public abstract class Interactable<T>
	: MonoBehaviour, IInteractable<T>, IInteractionProcessor<T>
	where T : Interaction
{
	//we delegate our interaction logic to this.
	private InteractionCoordinator<T> coordinator;

	protected void Start()
	{
		EnsureCoordinatorInit();
	}

	private void EnsureCoordinatorInit()
	{
		if (coordinator == null)
		{
			coordinator = new InteractionCoordinator<T>(this, WillInteract, ServerPerformInteraction);
		}
	}

	/// <summary>
	/// Decides if interaction logic should proceed. On client side, the interaction
	/// request will only be sent to the server if this returns true. On server side,
	/// the interaction will only be performed if this returns true.
	///
	/// Each interaction has a default implementation of this which should apply for most cases.
	/// By overriding this and adding more specific logic, you can reduce the amount of messages
	/// sent by the client to the server, decreasing overall network load.
	/// </summary>
	/// <param name="interaction">interaction to validate</param>
	/// <param name="side">which side of the network this is being invoked on</param>
	/// <returns>True/False based on whether the interaction logic should proceed as described above.</returns>
	protected virtual bool WillInteract(T interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}


	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	protected abstract void ServerPerformInteraction(T interaction);

	/// <summary>
	/// Client-side prediction. Called after validation succeeds on client side.
	/// Client can perform client side prediction. NOT invoked for server player, since there is no need
	/// for prediction.
	/// </summary>
	/// <param name="interaction"></param>
	protected virtual void ClientPredictInteraction(T interaction) { }

	public bool Interact(T info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.CoordinatedInteract(info, coordinator, ClientPredictInteraction);
	}

	public bool ServerProcessInteraction(T info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.ServerProcessCoordinatedInteraction(info, coordinator);
	}

}
