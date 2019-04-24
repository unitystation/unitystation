
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
/// for the interaction that this component wants to handle (such as MouseDropInfo for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
public abstract class CoordinatedInteraction<T>
	: MonoBehaviour, IInteractable<T>, IInteractionProcessor<T>
	where T : Interaction
{
	//we delegate our interaction logic to this.
	private InteractionCoordinator<T> coordinator;

	protected void Start()
	{
		coordinator = new InteractionCoordinator<T>(this, Validators(), ServerPerformInteraction);
		//subclasses must remember to call base.Start() if they use Start
	}

	/// <summary>
	/// Return the validators that should be used for this interaction for client/server validation.
	/// </summary>
	/// <returns>List of interaction validators to use for this interaction.</returns>
	protected abstract IList<IInteractionValidator<T>> Validators();

	/// <summary>
	/// Server-side. Called after validation succeeds on server side.
	/// Server should perform the interaction and inform clients as needed.
	/// </summary>
	/// <param name="interaction"></param>
	/// <returns>Currently should always be SOMETHING_HAPPENED, may be expanded later if needed.</returns>
	protected abstract InteractionResult ServerPerformInteraction(T interaction);

	public InteractionResult Interact(T info)
	{
		return coordinator.ClientValidateAndRequest(info);
	}

	public InteractionResult ServerProcessInteraction(T info)
	{
		return coordinator.ServerValidateAndPerform(info);
	}
}
