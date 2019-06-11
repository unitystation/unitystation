
/// <summary>
/// Validates if the performer is applying with an empty or non empty hand.
/// </summary>
public class IsHand : IInteractionValidator<HandApply>, IInteractionValidator<PositionalHandApply>
{
	public static readonly IsHand EMPTY = new IsHand(true);
	public static readonly IsHand OCCUPIED = new IsHand(false);

	private readonly bool shouldHandBeEmpty;

	private IsHand(bool shouldHandBeEmpty)
	{
		this.shouldHandBeEmpty = shouldHandBeEmpty;
	}


	public ValidationResult Validate(HandApply toValidate, NetworkSide side)
	{
		return toValidate.UsedObject == null == shouldHandBeEmpty ? ValidationResult.SUCCESS : ValidationResult.FAIL;
	}

	public ValidationResult Validate(PositionalHandApply toValidate, NetworkSide side)
	{
		return toValidate.UsedObject == null == shouldHandBeEmpty ? ValidationResult.SUCCESS : ValidationResult.FAIL;
	}
}
