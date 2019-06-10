
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Version of Interactable which supports Activate and extends NetworkBehavior rather than MonoBehavior
/// </summary>
public abstract class NBActivateInteractable
	: NetworkBehaviour, IInteractable<Activate>, IInteractionProcessor<Activate>
{
	//we delegate our interaction logic to this.
	private InteractionCoordinator<Activate> coordinator;

	protected void Start()
	{
		EnsureCoordinatorInit();
	}

	private void EnsureCoordinatorInit()
	{
		if (coordinator == null)
		{
			coordinator = new InteractionCoordinator<Activate>(this, InteractionValidationChain(), ServerPerformInteraction);
		}
	}

	/// <summary>
	/// Return the validators that should be used for this interaction for client/server validation.
	/// </summary>
	/// <returns>List of interaction validators to use for this interaction.</returns>
	protected abstract InteractionValidationChain<Activate> InteractionValidationChain();

	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	protected abstract void ServerPerformInteraction(Activate interaction);

	/// <summary>
	/// Client-side prediction. Called after validation succeeds on client side.
	/// Client can perform client side prediction. NOT invoked for server player, since there is no need
	/// for prediction.
	/// </summary>
	/// <param name="interaction"></param>
	protected virtual void ClientPredictInteraction(Activate interaction) { }

	/// <summary>
	/// Called on the server if server validation fails. Server can use this to inform client they should rollback any predictions they made.
	/// </summary>
	/// <param name="interaction"></param>
	protected virtual void OnServerInteractionValidationFail(Activate interaction) { }

	public InteractionControl Interact(Activate info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.CoordinatedInteract(info, coordinator, ClientPredictInteraction);
	}

	public InteractionControl ServerProcessInteraction(Activate info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.ServerProcessCoordinatedInteraction(info, coordinator, OnServerInteractionValidationFail);
	}
}
