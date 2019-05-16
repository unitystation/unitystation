
using UnityEngine;

/// <summary>
/// Validates if the dropped object has a specific component
/// </summary>
public class DoesDroppedObjectHaveComponent<T> : IInteractionValidator<MouseDrop>
	where T : Component
{
	public static readonly DoesDroppedObjectHaveComponent<T> DOES = new DoesDroppedObjectHaveComponent<T>();

	private DoesDroppedObjectHaveComponent()
	{
	}


	public ValidationResult Validate(MouseDrop toValidate, NetworkSide side)
	{
		return toValidate.UsedObject.GetComponent<T>() != null ?
			ValidationResult.SUCCESS :
			ValidationResult.FAIL;
	}
}
