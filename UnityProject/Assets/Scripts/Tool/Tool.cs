
using UnityEngine;

/// <summary>
/// Component that indicates an object's tool usage stats.
/// </summary>
public class Tool : MonoBehaviour
{
	[Tooltip("The probability that the tool will succeed")]
	public int SuccessChance = 100;

	[Tooltip("Used for syndicate only actions E.G disassembling self-destruct or SM crystal harvesting")]
	public bool IsSyndicateVariant = false;

	[Tooltip("Used to set a multiplier of how fast the tool will do an action")]
	public float SpeedMultiplier = 1;
}
