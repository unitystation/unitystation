using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Used to change the current stage, it specifies the tool how it would take what stage to go to, those Type of things
/// </summary>
[System.Serializable]
public class StageAdvance 
{
	/// <summary>
	/// the tool Needed
	/// </summary>
	public ToolType RequiredTool;

	/// <summary>
	/// How long it will take ( can be changed by speed multipliers on the tools )
	/// </summary>
	public float ConstructionTime;

	/// <summary>
	/// What stage to go to When this is activated
	/// </summary>
	public int JumpToStage;

	/// <summary>
	/// The probability that this will be successful (can be modified by tool success chances)
	/// </summary>
	public int SuccessChance = 100;

	public string SuccessText;
	public string FailedText;

	/// <summary>
	/// If true then require all the components in needed parts to be present otherwise  Ignore it
	/// </summary>
	public bool Construction;
}
