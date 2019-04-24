
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Version of <see cref="CoordinatedInteraction{T}"/> which supports 2
/// interaction types.
///
/// </summary>
/// <typeparamref name="T">Interaction subtype
/// for the interaction that this component wants to handle (such as MouseDropInfo for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
/// /// <typeparamref name="T2">Second interaction subtype
/// for the interaction that this component wants to handle (such as MouseDropInfo for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
public abstract class CoordinatedInteraction<T,T2>
	: CoordinatedInteraction<T>, IInteractable<T2>, IInteractionProcessor<T2>
	where T : Interaction
	where T2 : Interaction
{
	//we delegate our interaction logic to this.
	private InteractionCoordinator<T2> coordinator;

	protected void Start()
	{
		coordinator = new InteractionCoordinator<T2>(this, ValidatorsT2(), ServerPerformInteraction);
		//subclasses must remember to call base.Start() if they use Start
		base.Start();
	}

	/// <summary>
	/// Return the validators that should be used for this interaction for client/server validation.
	/// </summary>
	/// <returns>List of interaction validators to use for this interaction.</returns>
	protected abstract IList<IInteractionValidator<T2>> ValidatorsT2();

	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	/// <returns>Currently should always be SOMETHING_HAPPENED, may be expanded later if needed.</returns>
	protected abstract InteractionResult ServerPerformInteraction(T2 interaction);

	public InteractionResult Interact(T2 info)
	{
		return coordinator.ClientValidateAndRequest(info);
	}

	public InteractionResult ServerProcessInteraction(T2 info)
	{
		return coordinator.ServerValidateAndPerform(info);
	}
}
