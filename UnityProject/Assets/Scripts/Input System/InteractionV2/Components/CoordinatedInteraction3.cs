
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Version of <see cref="CoordinatedInteraction{T}"/> which supports 3
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
public abstract class CoordinatedInteraction<T,T2,T3>
	: CoordinatedInteraction<T,T2>, IInteractable<T3>, IInteractionProcessor<T3>
	where T : Interaction
	where T2 : Interaction
	where T3 : Interaction
{
	//we delegate our interaction logic to this.
	private InteractionCoordinator<T3> coordinator;

	protected void Start()
	{
		coordinator = new InteractionCoordinator<T3>(this, ValidatorsT3(), ServerPerformInteraction);
		//subclasses must remember to call base.Start() if they use Start
		base.Start();
	}

	/// <summary>
	/// Return the validators that should be used for this interaction for client/server validation.
	/// </summary>
	/// <returns>List of interaction validators to use for this interaction.</returns>
	protected abstract IList<IInteractionValidator<T3>> ValidatorsT3();

	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	/// <returns>Currently should always be SOMETHING_HAPPENED, may be expanded later if needed.</returns>
	protected abstract InteractionResult ServerPerformInteraction(T3 interaction);

	public InteractionResult Interact(T3 info)
	{
		return coordinator.ClientValidateAndRequest(info);
	}

	public InteractionResult ServerProcessInteraction(T3 info)
	{
		return coordinator.ServerValidateAndPerform(info);
	}
}
