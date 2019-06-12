
using UnityEngine;

/// <summary>
/// Validates if the used object is a tool of a specific type
/// </summary>
public class IsToolUsed :
	IInteractionValidator<MouseDrop>,
	IInteractionValidator<PositionalHandApply>,
	IInteractionValidator<HandApply>,
	IInteractionValidator<HandActivate>,
	IInteractionValidator<AimApply>,
	IInteractionValidator<InventoryApply>
{
	private ToolType expectedType;

	private IsToolUsed(ToolType expectedType)
	{
		this.expectedType = expectedType;
	}

	private ValidationResult ValidateAll(Interaction toValidate, NetworkSide side)
	{
		if (toValidate.UsedObject == null)
		{
			return ValidationResult.FAIL;
		}

		var tool = toValidate.UsedObject.GetComponent<Tool>();
		if (tool != null)
		{
			return tool.ToolType == expectedType ? ValidationResult.SUCCESS : ValidationResult.FAIL;
		}

		return ValidationResult.FAIL;
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

	/// <summary>
	/// Validates that the used object is a tool of the specified type
	/// </summary>
	/// <param name="expectedType"></param>
	/// <returns></returns>
	public static IInteractionValidator<HandApply> OfType(ToolType expectedType)
	{
		return new IsToolUsed(expectedType);
	}
}
