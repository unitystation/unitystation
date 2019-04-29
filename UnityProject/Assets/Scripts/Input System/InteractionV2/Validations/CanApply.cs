
using UnityEngine;

/// <summary>
/// Validates if the performer is in range and not in crit, which are typical requirements for all
/// hand apply interactions.
/// </summary>
public class CanApply : IInteractionValidator<HandApply>, IInteractionValidator<MouseDrop>
{
	public static readonly CanApply EVEN_IF_SOFT_CRIT = new CanApply(true);
	public static readonly CanApply ONLY_IF_CONSCIOUS = new CanApply(false);

	private readonly bool allowSoftCrit;

	private CanApply(bool allowSoftCrit)
	{
		this.allowSoftCrit = allowSoftCrit;
	}

	private ValidationResult ValidateAll(TargetedInteraction toValidate, NetworkSide side)
	{
		return Validate(toValidate.Performer, toValidate.TargetObject, allowSoftCrit) ? ValidationResult.SUCCESS : ValidationResult.FAIL;
	}

	public ValidationResult Validate(HandApply toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate, side);
	}

	public ValidationResult Validate(MouseDrop toValidate, NetworkSide side)
	{
		return ValidateAll(toValidate, side);
	}

	/// <summary>
	/// Perform the validation in a static context
	/// </summary>
	/// <param name="player">object of player trying to initiate the action</param>
	/// <param name="target">object being targeted</param>
	/// <param name="allowSoftCrit">whether to allow soft crit</param>
	/// <param name="ignoreRange">ignore range - allow interaciton even if out of range</param>
	/// <returns></returns>
	public static bool Validate(GameObject player, GameObject target, bool allowSoftCrit, bool ignoreRange = false)
	{
		var playerScript = player.GetComponent<PlayerScript>();

		if (playerScript.canNotInteract() && (!playerScript.playerHealth.IsSoftCrit || !allowSoftCrit))
		{
			return false;
		}

		return ignoreRange || playerScript.IsInReach(target.transform.position, false);
	}
}
