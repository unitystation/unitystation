
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Version of Interactable which supports HandApply and extends NetworkBehavior rather than MonoBehavior
/// </summary>
public abstract class NBHandApplyInteractable
	: NetworkBehaviour, IInteractable<HandApply>, IInteractionProcessor<HandApply>
{
	//we delegate our interaction logic to this.
	private InteractionCoordinator<HandApply> coordinator;

	protected void Start()
	{
		coordinator = new InteractionCoordinator<HandApply>(this, Validators(), ServerPerformInteraction);
		//subclasses must remember to call base.Start() if they use Start
	}

	/// <summary>
	/// Return the validators that should be used for this interaction for client/server validation.
	/// </summary>
	/// <returns>List of interaction validators to use for this interaction.</returns>
	protected abstract IList<IInteractionValidator<HandApply>> Validators();

	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	/// <returns>Currently should always be SOMETHING_HAPPENED, may be expanded later if needed.</returns>
	protected abstract InteractionResult ServerPerformInteraction(HandApply interaction);

	public InteractionResult Interact(HandApply info)
	{
		return coordinator.ClientValidateAndRequest(info);
	}

	public InteractionResult ServerProcessInteraction(HandApply info)
	{
		return coordinator.ServerValidateAndPerform(info);
	}
}
