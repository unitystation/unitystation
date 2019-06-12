
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Version of <see cref="Interactable{T}"/> which supports 2
/// interaction types.
///
/// </summary>
/// <typeparamref name="T">Interaction subtype
/// for the interaction that this component wants to handle (such as MouseDrop for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
/// /// <typeparamref name="T2">Second interaction subtype
/// for the interaction that this component wants to handle (such as MouseDrop for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
public abstract class Interactable<T,T2>
	: Interactable<T>, IInteractable<T2>, IInteractionProcessor<T2>
	where T : Interaction
	where T2 : Interaction
{
	//we delegate our interaction logic to this.
	private InteractionCoordinator<T2> coordinator;

	protected void Start()
	{
		EnsureCoordinatorInit();
		//subclasses must remember to call base.Start() if they use Start
		base.Start();
	}

	private void EnsureCoordinatorInit()
	{
		if (coordinator == null)
		{
			coordinator = new InteractionCoordinator<T2>(this, InteractionValidationChainT2(), ServerPerformInteraction);
		}
	}

	/// <summary>
	/// Return the validators that should be used for this interaction for client/server validation.
	/// </summary>
	/// <returns>List of interaction validators to use for this interaction.</returns>
	protected abstract InteractionValidationChain<T2> InteractionValidationChainT2();

	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	/// <returns>Currently should always be SOMETHING_HAPPENED, may be expanded later if needed.</returns>
	protected abstract void ServerPerformInteraction(T2 interaction);

	/// <summary>
	/// Client-side prediction. Called after validation succeeds on client side.
	/// Client can perform client side prediction. NOT invoked for server player, since there is no need
	/// for prediction.
	/// </summary>
	/// <param name="interaction"></param>
	protected virtual void ClientPredictInteraction(T2 interaction) { }

	/// <summary>
	/// Called on the server if server validation fails. Server can use this to inform client they should rollback any predictions they made.
	/// </summary>
	/// <param name="interaction"></param>
	protected virtual void OnServerInteractionValidationFail(T2 interaction) { }

	public InteractionControl Interact(T2 info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.CoordinatedInteract(info, coordinator, ClientPredictInteraction);
	}

	public InteractionControl ServerProcessInteraction(T2 info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.ServerProcessCoordinatedInteraction(info, coordinator, OnServerInteractionValidationFail);
	}
}
