using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class StageAdvance 
{
	public ToolType RequiredTool;
	public float ConstructionTime;
	public int JumpToStage;
	public int SuccessChance = 100;
	public string SuccessText;
	public string FailedText;
	public bool Construction;
}
