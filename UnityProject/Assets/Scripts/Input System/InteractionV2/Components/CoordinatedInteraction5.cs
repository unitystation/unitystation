
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Version of <see cref="CoordinatedInteraction{T}"/> which supports 5
/// interaction types.
///
/// </summary>
/// <typeparamref name="T">Interaction subtype
/// for the interaction that this component wants to handle (such as MouseDropInfo for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
/// <typeparamref name="T2">Second interaction subtype
/// for the interaction that this component wants to handle (such as MouseDropInfo for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
/// <typeparamref name="T3">Third interaction subtype
/// for the interaction that this component wants to handle (such as MouseDropInfo for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
/// <typeparamref name="T4">fourth interaction subtype
/// for the interaction that this component wants to handle (such as MouseDropInfo for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
/// <typeparamref name="T5">fourth interaction subtype
/// for the interaction that this component wants to handle (such as MouseDropInfo for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
public abstract class CoordinatedInteraction<T,T2,T3,T4,T5>
	: CoordinatedInteraction<T,T2,T3,T4>, IInteractable<T5>, IInteractionProcessor<T5>
	where T : Interaction
	where T2 : Interaction
	where T3 : Interaction
	where T4 : Interaction
	where T5 : Interaction
{
	//we delegate our interaction logic to this.
	private InteractionCoordinator<T5> coordinator;

	protected void Start()
	{
		coordinator = new InteractionCoordinator<T5>(this, ValidatorsT5(), ServerPerformInteraction);
		//subclasses must remember to call base.Start() if they use Start
		base.Start();
	}

	/// <summary>
	/// Return the validators that should be used for this interaction for client/server validation.
	/// </summary>
	/// <returns>List of interaction validators to use for this interaction.</returns>
	protected abstract IList<IInteractionValidator<T5>> ValidatorsT5();

	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	/// <returns>Currently should always be SOMETHING_HAPPENED, may be expanded later if needed.</returns>
	protected abstract InteractionResult ServerPerformInteraction(T5 interaction);

	public InteractionResult Interact(T5 info)
	{
		return coordinator.ClientValidateAndRequest(info);
	}

	public InteractionResult ServerProcessInteraction(T5 info)
	{
		return coordinator.ServerValidateAndPerform(info);
	}
}
