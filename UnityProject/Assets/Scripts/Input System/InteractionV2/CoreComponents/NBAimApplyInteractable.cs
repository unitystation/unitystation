
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Version of Interactable which supports AimApply and extends NetworkBehavior rather than MonoBehavior
/// </summary>
public abstract class NBAimApplyInteractable
	: NetworkBehaviour, IInteractable<AimApply>, IInteractionProcessor<AimApply>
{
	//we delegate our interaction logic to this.
	private InteractionCoordinator<AimApply> coordinator;

	protected void Start()
	{
		EnsureCoordinatorInit();
	}

	private void EnsureCoordinatorInit()
	{
		if (coordinator == null)
		{
			coordinator = new InteractionCoordinator<AimApply>(this, InteractionValidationChain(), ServerPerformInteraction);
		}
	}

	/// <summary>
	/// Return the validators that should be used for this interaction for client/server validation.
	/// </summary>
	/// <returns>List of interaction validators to use for this interaction.</returns>
	protected abstract InteractionValidationChain<AimApply> InteractionValidationChain();

	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	protected abstract void ServerPerformInteraction(AimApply interaction);

	/// <summary>
	/// Client-side prediction. Called after validation succeeds on client side.
	/// Client can perform client side prediction. NOT invoked for server player, since there is no need
	/// for prediction.
	/// </summary>
	/// <param name="interaction"></param>
	protected virtual void ClientPredictInteraction(AimApply interaction) { }

	/// <summary>
	/// Called on the server if server validation fails. Server can use this to inform client they should rollback any predictions they made.
	/// </summary>
	/// <param name="interaction"></param>
	protected virtual void OnServerInteractionValidationFail(AimApply interaction) { }

	public InteractionControl Interact(AimApply info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.CoordinatedInteract(info, coordinator, ClientPredictInteraction);
	}

	public InteractionControl ServerProcessInteraction(AimApply info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.ServerProcessCoordinatedInteraction(info, coordinator, OnServerInteractionValidationFail);
	}
}
