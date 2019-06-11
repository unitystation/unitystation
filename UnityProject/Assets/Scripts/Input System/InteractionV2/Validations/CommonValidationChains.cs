
/// <summary>
/// Provides common validation chains so they can be reused for different interactables and not consume memory.
/// </summary>
public static class CommonValidationChains
{
	public static readonly InteractionValidationChain<HandApply> CAN_APPLY_HAND_SOFT_CRIT
		= InteractionValidationChain<HandApply>.Create()
			.WithValidation(CanApply.EVEN_IF_SOFT_CRIT);
	public static readonly InteractionValidationChain<HandApply> CAN_APPLY_HAND_CONSCIOUS
		= InteractionValidationChain<HandApply>.Create()
			.WithValidation(CanApply.ONLY_IF_CONSCIOUS);

}
