
using UnityEngine;

/// <summary>
/// Ensures that the target object is a specific gameobject.
/// </summary>
public class TargetIs : IInteractionValidator<HandApply>, IInteractionValidator<PositionalHandApply>
{
	private readonly GameObject expectedTarget;

	private TargetIs(GameObject expectedTarget)
	{
		this.expectedTarget = expectedTarget;
	}

	private ValidationResult ValidateAll(TargetedInteraction targetedInteraction, NetworkSide side)
	{
		return targetedInteraction.TargetObject == expectedTarget ?
			ValidationResult.SUCCESS :
			ValidationResult.FAIL;
	}

	public ValidationResult Validate(HandApply toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate, side);
	}

	public ValidationResult Validate(PositionalHandApply toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate, side);
	}

	/// <summary>
	/// Validate that the target of the interaction is a specific game object
	/// </summary>
	/// <param name="expectedTarget">expected gameobject targeted by the hand apply</param>
	/// <returns></returns>
	public static TargetIs GameObject(GameObject expectedTarget)
	{
		return new TargetIs(expectedTarget);
	}
}
