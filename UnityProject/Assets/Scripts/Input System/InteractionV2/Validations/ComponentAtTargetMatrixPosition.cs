
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Validates if any component of any object at the targeted object's position in the matrix meets some
/// criteria.
/// </summary>
public class ComponentAtTargetMatrixPosition<T> : IInteractionValidator<MouseDrop>, IInteractionValidator<HandApply>
	where T : MonoBehaviour
{
	private readonly Func<T, bool> criteria;
	private readonly bool shouldAnyMatch;

	private ComponentAtTargetMatrixPosition(Func<T, bool> criteria, bool shouldAnyMatch)
	{
		this.criteria = criteria;
		this.shouldAnyMatch = shouldAnyMatch;
	}


	private ValidationResult AllValidate(TargetedInteraction toValidate, NetworkSide side)
	{
		var position = toValidate.TargetObject.transform.position.CutToInt();
		return MatrixManager.GetAt<T>(position, side == NetworkSide.SERVER).Any(criteria) == shouldAnyMatch ?
			ValidationResult.SUCCESS :
			ValidationResult.FAIL;
	}

	public ValidationResult Validate(MouseDrop toValidate, NetworkSide side)
	{
		return AllValidate(toValidate, side);
	}

	public ValidationResult Validate(HandApply toValidate, NetworkSide side)
	{
		return AllValidate(toValidate, side);
	}

	/// <summary>
	/// Validation succeeds if there is any object at the targeted object's position in the matrix
	/// with the specified component where the component matches the criteria.
	/// </summary>
	/// <param name="criteria">criteria to check on the component</param>
	/// <returns></returns>
	public static ComponentAtTargetMatrixPosition<T> MatchingCriteria(Func<T,bool> criteria)
	{
		return new ComponentAtTargetMatrixPosition<T>(criteria, true);
	}

	/// <summary>
	/// Validation succeeds if there is NO object at the targeted object's position in the matrix
	/// with the specified component where the component matches the criteria.
	/// </summary>
	/// <param name="criteria">criteria to check on the component</param>
	/// <returns></returns>
	public static ComponentAtTargetMatrixPosition<T> NoneMatchingCriteria(Func<T,bool> criteria)
	{
		return new ComponentAtTargetMatrixPosition<T>(criteria, false);
	}


}
