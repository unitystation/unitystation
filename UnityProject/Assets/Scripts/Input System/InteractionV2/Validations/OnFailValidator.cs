

using System;

/// <summary>
/// Validator which invokes some validator and invokes a method when that particular validation
/// fails during a validation check. This will only be invoked on the side (client or server)
/// that validation fails - if it fails client side, the onFail will be invoked on client side but
/// will not be invoked on the server.
/// </summary>
/// <typeparam name="T"></typeparam>
public class OnFailValidator<T> : IInteractionValidator<T>
	where T : Interaction
{
	private IInteractionValidator<T> validator;
	private Action<T, NetworkSide> onFail;

	/// <summary>
	///
	/// </summary>
	/// <param name="validator">validator to use</param>
	/// <param name="onFail">action to invoke when validation fails, passing it the interaction
	/// that failed.</param>
	public OnFailValidator(IInteractionValidator<T> validator, Action<T, NetworkSide> onFail)
	{
		this.validator = validator;
		this.onFail = onFail;
	}

	public ValidationResult Validate(T toValidate, NetworkSide side)
	{
		var result = validator.Validate(toValidate, side);
		if (result == ValidationResult.FAIL)
		{
			onFail.Invoke(toValidate, side);
		}

		return result;
	}
}
