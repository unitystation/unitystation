
using System;

/// <summary>
/// Helper class for coordinating an interaction between client / server.
///
/// You can delegate to this in your component to simplify the interaction logic if your interaction logic follows
/// this flow:
/// 1. Client validates that interaction will happen.
/// 2. Client sends RequestInteractMessage to server if validation succeeds.
/// 3. Server validates the interaction.
/// 4. Server updates its own state and informs whatever clients need to be informed
/// </summary>
/// <typeparamref name="T">Interaction subtype
/// for the interaction that this component wants to handle (such as MouseDropInfo for a mouse drop interaction).
/// Must be a subtype of Interaction.</typeparamref>
public class InteractionCoordinator<T>
	where T : Interaction
{
	//note: using enums rather than bool for these so it's more clear what the
	//delegate functions need to do.

	private readonly Func<T, NetworkSide, ValidationResult> validationLogic;
	private readonly Func<T, InteractionResult> interactionLogic;
	private readonly IInteractionProcessor<T> processor;

	/// <summary>
	/// Create a coordinator which will handle the interaction logic
	/// </summary>
	/// <param name="processor">Should almost always be "this" - the component using this coordinator should
	/// implement IInteractionProcessor and delegate processing to this coordinator.
	/// Component which will process the interaction on the server side.</param>
	/// <param name="validationLogic">function which will validate the interaction on client and server side.</param>
	/// <param name="interactionLogic">Function to invoke on the server side to perform the interaction logic
	/// if validation succeeds.</param>
	public InteractionCoordinator(IInteractionProcessor<T> processor, Func<T, NetworkSide, ValidationResult> validationLogic,
		Func<T, InteractionResult> interactionLogic)
	{
		this.validationLogic = validationLogic;
		this.processor = processor;
		this.interactionLogic = interactionLogic;
	}

	/// <summary>
	/// Coordinator will perform client side validation of the interaction using its validation function.
	/// If validation succeeds, it will send RequestInteractMessage to the server to request the server
	/// to perform the interaction.
	/// </summary>
	/// <param name="interaction">interaction being performed.</param>
	/// <returns>true if interaction validated and message was sent, false otherwise</returns>
	public InteractionResult ClientValidateAndRequest(T interaction)
	{
		if (validationLogic.Invoke(interaction, NetworkSide.CLIENT) == ValidationResult.SUCCESS)
        {
        	RequestInteractMessage.Send(interaction, processor);

            return InteractionResult.SOMETHING_HAPPENED;
        }

		return InteractionResult.NOTHING_HAPPENED;
	}

	/// <summary>
	/// Perform the server side validation and perform the interaction logic
	/// if validation succeeds.
	/// </summary>
	/// <param name="interaction">info on the interaction being requested</param>
	/// <returns>NOTHING_HAPPENED if validation fails, otherwise returns the result of the
	/// interaction./returns>
	/// <exception cref="NotImplementedException"></exception>
	public InteractionResult ServerValidateAndPerform(T interaction)
	{
		if (validationLogic.Invoke(interaction, NetworkSide.SERVER) == ValidationResult.SUCCESS)
		{
			return interactionLogic.Invoke(interaction);
		}

		return InteractionResult.NOTHING_HAPPENED;
	}
}

/// <summary>
/// Refers to a "side" of the network - client or server.
/// </summary>
public enum NetworkSide
{
	CLIENT,
	SERVER
}

/// <summary>
/// Result of validation of an interaction.
/// </summary>
public enum ValidationResult
{
	//validation failed - interaction should not be performed
	FAIL,
	//validation succeeded, proceed with the interaction
	SUCCESS
}
