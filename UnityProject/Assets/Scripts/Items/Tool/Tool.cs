
using UnityEngine;

/// <summary>
/// Component that indicates an object's tool usage stats.
/// </summary>
public class Tool : MonoBehaviour
{
	[Tooltip("Used to set a multiplier of how fast the tool will do an action")]
	public float SpeedMultiplier = 1;

	[Tooltip("Percentage chance to Succeed as 100 to 0")]
	public float PercentageChance = 100;

	//TODO Set this out to pick out via related item attribute, requires passing the related item attribute every time tool util is used with Tool Component
	// [Tooltip("The related item Trait this tool component is about, only needed if you have multiple")]
	// public ItemTrait RelatedItemTrait;

}
