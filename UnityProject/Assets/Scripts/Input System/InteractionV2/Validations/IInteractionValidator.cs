
/// <summary>
/// Indicates a component which can perform some validation on an interaction. Most implementers
/// should implement the singleton pattern if they have no configuration / state, and
/// a static factory pattern if they do have state. Try to name them so that the validation classes
/// and static factories are self-describing.
///
/// Validators can be chained together to create the full validation logic for an interaction.
/// </summary>
/// <typeparamref name="T">Interaction subtype
/// for the interaction that this component wants to validate.</typeparamref>
public interface IInteractionValidator<T>
	where T : Interaction
{

	ValidationResult Validate(T toValidate, NetworkSide side);
}
