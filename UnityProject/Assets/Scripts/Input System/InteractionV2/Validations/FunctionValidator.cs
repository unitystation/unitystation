
using System;

/// <summary>
/// General purpose validator which allows supplying a specific validation function.
///
/// Only use this if a particular validation will only be used in one component. Otherwise, you
/// should create your own validator to prevent duplication of that logic.
/// </summary>
/// <typeparam name="T"></typeparam>
public class FunctionValidator<T> : IInteractionValidator<T>
	where T : Interaction
{
	private readonly Func<T, NetworkSide, ValidationResult> validation;

	/// <summary>
	///
	/// </summary>
	/// <param name="validation">validation function</param>
	public FunctionValidator(Func<T, NetworkSide, ValidationResult> validation)
	{
		this.validation = validation;
	}

	public ValidationResult Validate(T toValidate, NetworkSide side)
	{
		return validation.Invoke(toValidate, side);
	}
}
