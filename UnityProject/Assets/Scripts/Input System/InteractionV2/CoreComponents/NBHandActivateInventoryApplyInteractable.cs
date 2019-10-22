using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Version of Interactable which uses InventoryApply and HandActivate
/// </summary>
public abstract class NBHandActivateInventoryApplyInteractable
	: NetworkBehaviour, IInteractable<InventoryApply>, IInteractionProcessor<InventoryApply>, IInteractable<HandActivate>, IInteractionProcessor<HandActivate>
{
	//we delegate our interaction logic to this.
	private InteractionCoordinator<InventoryApply> coordinatorInventoryApply;
	private InteractionCoordinator<HandActivate> coordinatorHandActivate;

	protected void Start()
	{
		EnsureCoordinatorInit();
	}

	private void EnsureCoordinatorInit()
	{
		if (coordinatorInventoryApply == null)
		{
			coordinatorInventoryApply = new InteractionCoordinator<InventoryApply>(this, WillInteract, ServerPerformInteraction);
		}
		if (coordinatorHandActivate == null)
		{
			coordinatorHandActivate = new InteractionCoordinator<HandActivate>(this, WillInteract, ServerPerformInteraction);
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
	protected virtual bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	protected abstract void ServerPerformInteraction(InventoryApply interaction);

	/// <summary>
	/// Client-side prediction. Called after validation succeeds on client side.
	/// Client can perform client side prediction. NOT invoked for server player, since there is no need
	/// for prediction.
	/// </summary>
	/// <param name="interaction"></param>
	protected virtual void ClientPredictInteraction(InventoryApply interaction) { }

	public bool Interact(InventoryApply info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.CoordinatedInteract(info, coordinatorInventoryApply, ClientPredictInteraction);
	}

	public bool ServerProcessInteraction(InventoryApply info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.ServerProcessCoordinatedInteraction(info, coordinatorInventoryApply);
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
	protected virtual bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	protected abstract void ServerPerformInteraction(HandActivate interaction);

	/// <summary>
	/// Client-side prediction. Called after validation succeeds on client side.
	/// Client can perform client side prediction. NOT invoked for server player, since there is no need
	/// for prediction.
	/// </summary>
	/// <param name="interaction"></param>
	protected virtual void ClientPredictInteraction(HandActivate interaction) { }

	public bool Interact(HandActivate info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.CoordinatedInteract(info, coordinatorHandActivate, ClientPredictInteraction);
	}

	public bool ServerProcessInteraction(HandActivate info)
	{
		EnsureCoordinatorInit();
		return InteractionComponentUtils.ServerProcessCoordinatedInteraction(info, coordinatorHandActivate);
	}
}
