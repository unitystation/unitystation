
using UnityEngine;

/// <summary>
/// Validates if the target object has a specific component
/// </summary>
public class DoesTargetObjectHaveComponent<T> :
	IInteractionValidator<MouseDrop>,
	IInteractionValidator<HandApply>,
	IInteractionValidator<PositionalHandApply>,
	IInteractionValidator<InventoryApply>
	where T : Component
{
	public static readonly DoesTargetObjectHaveComponent<T> DOES = new DoesTargetObjectHaveComponent<T>();

	private DoesTargetObjectHaveComponent()
	{
	}

	private ValidationResult ValidateAll(GameObject target, NetworkSide side)
	{
		return target != null && target.GetComponent<T>() != null ? ValidationResult.SUCCESS : ValidationResult.FAIL;
	}


	public ValidationResult Validate(MouseDrop toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate.TargetObject, side);
	}

	public ValidationResult Validate(HandApply toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate.TargetObject, side);
	}

	public ValidationResult Validate(InventoryApply toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate.TargetObject, side);
	}

	public ValidationResult Validate(PositionalHandApply toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate.TargetObject, side);
	}
}
