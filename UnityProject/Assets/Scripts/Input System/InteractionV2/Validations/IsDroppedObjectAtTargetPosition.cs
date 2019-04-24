
/// <summary>
/// Validates if the dropped object is at the target object's position (rounded to matrix position)
/// </summary>
public class IsDroppedObjectAtTargetPosition : IInteractionValidator<MouseDrop>
{
	public static readonly IsDroppedObjectAtTargetPosition IS = new IsDroppedObjectAtTargetPosition();

	private IsDroppedObjectAtTargetPosition()
	{
	}


	public ValidationResult Validate(MouseDrop toValidate, NetworkSide side)
	{
		return toValidate.UsedObject.transform.position.CutToInt() == toValidate.TargetObject.transform.position.CutToInt() ?
			ValidationResult.SUCCESS :
			ValidationResult.FAIL;
	}
}
