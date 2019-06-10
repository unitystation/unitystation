
using UnityEngine;

/// <summary>
/// Component that indicates an object can be used as a tool. Various other components
/// have interaction logic that checks if a certain kind of tool is being used on them.
/// </summary>
public class Tool : MonoBehaviour
{
	[Tooltip("Type of tool this object functions as")]
	public ToolType ToolType;
}

public enum ToolType
{
	Crowbar = 0,
	Wirecutter = 1,
	Wrench = 2
}
