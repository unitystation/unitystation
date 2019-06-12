
using UnityEngine;

/// <summary>
/// Validates if the used object has a specific component
/// </summary>
public class DoesUsedObjectHaveComponent<T> :
	IInteractionValidator<MouseDrop>,
	IInteractionValidator<HandApply>,
	IInteractionValidator<PositionalHandApply>,
	IInteractionValidator<HandActivate>,
	IInteractionValidator<AimApply>,
	IInteractionValidator<InventoryApply>
	where T : Component
{
	public static readonly DoesUsedObjectHaveComponent<T> DOES = new DoesUsedObjectHaveComponent<T>();

	private DoesUsedObjectHaveComponent()
	{
	}

	private ValidationResult ValidateAll(Interaction toValidate, NetworkSide side)
	{
		return toValidate.UsedObject != null && toValidate.UsedObject.GetComponent<T>() != null ? ValidationResult.SUCCESS : ValidationResult.FAIL;
	}


	public ValidationResult Validate(MouseDrop toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate, side);
	}

	public ValidationResult Validate(HandApply toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate, side);
	}

	public ValidationResult Validate(PositionalHandApply toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate, side);
	}

	public ValidationResult Validate(HandActivate toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate, side);
	}

	public ValidationResult Validate(AimApply toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate, side);
	}

	public ValidationResult Validate(InventoryApply toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate, side);
	}
}
