
using System;
using System.Collections.Generic;

/// <summary>
/// Represents a sequence of IInteractionValidators to use to validate an interaction.
///
/// Immutable, to encourage re-use / sharing to avoid consuming lots of memory with lots of validators.
/// </summary>
public class InteractionValidationChain<T>
	where T : Interaction
{
	public static readonly InteractionValidationChain<T> EMPTY = new InteractionValidationChain<T>(null, null);

	private readonly IInteractionValidator<T> validation;
	private readonly InteractionValidationChain<T> previous;

	private InteractionValidationChain(IInteractionValidator<T> validation, InteractionValidationChain<T> previous)
	{
		this.validation = validation;
		this.previous = previous;
	}

	/// <summary>
	/// Validates the specified interaction using the validations in this chain
	/// </summary>
	/// <param name="interaction"></param>
	/// <param name="networkSide">whether to do client-side or server-side validation. Server-side validation
	/// should only be used when the server is validating a client's attempt to perform an interaction.</param>
	/// <returns>result of validation</returns>
	public ValidationResult Validate(T interaction, NetworkSide networkSide)
	{
		//DFS so we don't blow up the stack
		var curChain = this;
		do
		{
			if (curChain.validation != null && curChain.validation.Validate(interaction, networkSide) == ValidationResult.FAIL)
			{
				return ValidationResult.FAIL;
			}

			curChain = curChain.previous;
		}
		while (curChain != null);

		return ValidationResult.SUCCESS;
	}

	/// <summary>
	/// Validates the specified interaction using the validations in this chain, returning true if it validates.
	/// Shorthand for checking if Validate == ValidationResult.SUCCESS
	/// </summary>
	/// <param name="interaction"></param>
	/// <param name="networkSide">whether to do client-side or server-side validation. Server-side validation
	/// should only be used when the server is validating a client's attempt to perform an interaction.</param>
	/// <returns>result of validation</returns>
	public bool DoesValidate(T interaction, NetworkSide networkSide)
	{
		return Validate(interaction, networkSide) == ValidationResult.SUCCESS;
	}

	/// <summary>
	/// Create a new empty validation chain.
	/// </summary>
	/// <returns></returns>
	public static InteractionValidationChain<T> Create()
	{
		return EMPTY;
	}

	/// <summary>
	/// Create a validation chain that only consists of the provided validation.
	/// </summary>
	/// <returns></returns>
	public static InteractionValidationChain<T> Create(IInteractionValidator<T> toAdd)
	{
		return new InteractionValidationChain<T>(toAdd, null);
	}

	/// <summary>
	/// Adds the specified interaction to the end of the chain
	/// </summary>
	/// <param name="toAdd"></param>
	/// <param name="onFail">invoked when this validation fails. This will only be invoked on the side (client or server)
	/// that validation fails - if it fails client side, the onFail will be invoked on client side but
	/// will not be invoked on the server.</param>
	/// <returns>this</returns>
	public InteractionValidationChain<T> WithValidation(IInteractionValidator<T> toAdd, Action<T, NetworkSide> onFail = null)
	{
		if (onFail != null)
		{
			toAdd = new OnFailValidator<T>(toAdd, onFail);
		}

		return new InteractionValidationChain<T>(toAdd, this);
	}

	/// <summary>
	/// Adds the specified interaction validation function to the end of the chain,
	/// shorthand for WithValidation(new FunctionValidator<T>(validationFunction))
	/// </summary>
	/// <returns>this</returns>
	public InteractionValidationChain<T> WithValidation(Func<T, NetworkSide, ValidationResult> validationFunction, Action<T, NetworkSide> onFail = null)
	{
		return WithValidation(new FunctionValidator<T>(validationFunction), onFail);
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
