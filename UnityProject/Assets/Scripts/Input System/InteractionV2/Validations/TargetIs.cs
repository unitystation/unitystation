
using UnityEngine;

/// <summary>
/// Ensures that the target object is a specific gameobject.
/// </summary>
public class TargetIs : IInteractionValidator<HandApply>
{
	private readonly GameObject expectedTarget;

	private TargetIs(GameObject expectedTarget)
	{
		this.expectedTarget = expectedTarget;
	}


	public ValidationResult Validate(HandApply toValidate, NetworkSide side)
	{
		return toValidate.TargetObject == expectedTarget ?
			ValidationResult.SUCCESS :
			ValidationResult.FAIL;
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
