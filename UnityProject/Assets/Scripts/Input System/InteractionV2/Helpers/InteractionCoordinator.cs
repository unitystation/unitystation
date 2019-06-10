
using System;
using System.Collections.Generic;

/// <summary>
/// Helper class for coordinating an interaction between client / server.
///
/// You can delegate to this in your component to simplify the interaction logic if your interaction logic follows
/// this flow:
/// 1. Client validates that interaction will happen (which may consist of several different validations).
/// 2. Client sends RequestInteractMessage to server if validation succeeds.
/// 3. Server validates the interaction (which may consist of several different validations).
/// 4. Server updates its own state and informs whatever clients need to be informed
/// </summary>
/// <typeparamref name="T">Interaction subtype
/// for the interaction that this component wants to handle (such as MouseDrop for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
public class InteractionCoordinator<T>
	where T : Interaction
{
	//note: using enums rather than bool for these so it's more clear what the
	//delegate functions need to do.

	private readonly InteractionValidationChain<T> validationChain;
	private readonly Action<T> interactionLogic;
	private readonly IInteractionProcessor<T> processor;

	/// <summary>
	/// Create a coordinator which will handle the interaction logic
	/// </summary>
	/// <param name="processor">Should almost always be "this" - the component using this coordinator should
	/// implement IInteractionProcessor and delegate processing to this coordinator.
	/// Component which will process the interaction on the server side.</param>
	/// <param name="validationChain">Validations to perform on client and server side. If any validation fails,
	/// the entire validation will fail. Can be null if there is no need for validations.</param>
	/// <param name="interactionLogic">Function to invoke on the server side to perform the interaction logic
	/// if validation succeeds.</param>
	public InteractionCoordinator(IInteractionProcessor<T> processor, InteractionValidationChain<T> validationChain,
		Action<T> interactionLogic)
	{
		this.validationChain = validationChain;
		this.processor = processor;
		this.interactionLogic = interactionLogic;
	}

	/// <summary>
	/// Create a coordinator which will handle the interaction logic. It is recommended to use the
	/// other constructor so you can use the re-usable IInteractionValidators and avoid code duplication.
	/// </summary>
	/// <param name="processor">Should almost always be "this" - the component using this coordinator should
	/// implement IInteractionProcessor and delegate processing to this coordinator.
	/// Component which will process the interaction on the server side.</param>
	/// <param name="validationLogic">function which will validate the interaction on client and server side.</param>
	/// <param name="interactionLogic">Function to invoke on the server side to perform the interaction logic
	/// if validation succeeds.</param>
	public InteractionCoordinator(IInteractionProcessor<T> processor, Func<T, NetworkSide, ValidationResult> validationLogic,
		Action<T> interactionLogic)
	{
		this.validationChain = InteractionValidationChain<T>.Create(new FunctionValidator<T>(validationLogic));
		this.processor = processor;
		this.interactionLogic = interactionLogic;
	}

	/// <summary>
	/// Coordinator will perform client side validation of the interaction using its validators. If any
	/// validator fails, validation will fail and NOTHING_HAPPENED will be returned.
	/// If validation succeeds, it will send RequestInteractMessage to the server to request the server
	/// to perform the interaction.
	/// </summary>
	/// <param name="interaction">interaction being performed.</param>
	/// <returns>whether validation succeeded or failed</returns>
	public ValidationResult ClientValidateAndRequest(T interaction)
	{
		if (validationChain == null || validationChain.Validate(interaction, NetworkSide.CLIENT) == ValidationResult.SUCCESS)
		{
			InteractionMessageUtils.SendRequest(interaction, processor);
			return ValidationResult.SUCCESS;
		}
		else
		{
			return ValidationResult.FAIL;
		}
	}

	/// <summary>
	/// Perform the server side validation and perform the interaction logic
	/// if validation succeeds. Validation fails if any of the validators fail.
	/// </summary>
	/// <param name="interaction">info on the interaction being requested</param>
	/// <returns>whether server validation succeeded or failed</returns>
	public ValidationResult ServerValidateAndPerform(T interaction)
	{
		if (validationChain == null || validationChain.Validate(interaction, NetworkSide.SERVER) == ValidationResult.SUCCESS)
		{
			interactionLogic.Invoke(interaction);
			return ValidationResult.SUCCESS;
		}
		else
		{
			return ValidationResult.FAIL;
		}
	}
}

