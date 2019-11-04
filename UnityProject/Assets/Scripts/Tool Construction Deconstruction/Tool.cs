
using UnityEngine;

/// <summary>
/// Component that indicates an object can be used as a tool. Various other components
/// have interaction logic that checks if a certain kind of tool is being used on them.
/// </summary>
public class Tool : MonoBehaviour
{
	[Tooltip("Type of tool this object functions as")]
	public ToolType ToolType;
	[Tooltip("The probability that the tool will succeed")]
	public int SuccessChance = 100;

	[Tooltip("Used for syndicate only actions E.G disassembling self-destruct or SM crystal harvesting")]
	public bool IsSyndicateVariant = false;

	[Tooltip("Used to set a multiplier of how fast the tool will do an action")]
	public float SpeedMultiplier = 1;
}

/// <summary>
/// Defines various tool types, a way of distinguishing items that's different from itemtype
/// </summary>
public enum ToolType
{
	//no tool has this type, just used for defining what's allowed to go in a slot
	None = -1,

	Crowbar = 0,
	Wirecutter = 1,
	Wrench = 2,
	Welder = 3,
	Screwdriver = 4,
	Multitool = 5,
	Emag = 6,
	Scalpel = 7,
	Retractor = 8,
	Saw = 9,
	Hemostat =  10,
	Cautery = 11,
	FireExtinguisher = 12,

	//no tool has this type, just used for defining what's allowed to go in a slot
	All = 2048
}
