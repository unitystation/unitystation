using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Stores data about the current stage of construction, For example what tools can change the stage, what they do, what parts are currently present, what parts are needed for construction and Other information
/// </summary>
[System.Serializable]
public class ConstructionStage 
{
	public Sprite StageSprite;

	/// <summary>
	/// What tools I needed to change a stage and what effects they have
	/// </summary>
	public List<StageAdvance> StageAdvances;

	/// <summary>
	/// What parts are needed for construction stages also what parts drop when you advance to the stage
	/// </summary>
	public List<ComponentData> NeededParts;

	/// <summary>
	/// when The fully constructed machine loads should this be included in the contained parts for NeededParts
	/// </summary>
	public bool IncludePartsInitialisation;

	/// <summary>
	/// What state the object should be in, In this stage
	/// </summary>
	public ObjectState ObjectStateofStage;

	[HideInInspector]
	public bool MissingParts;

	[HideInInspector]
	public List<GameObject> PresentParts;

	public Dictionary<ToolType, StageAdvance> ToolStage = new Dictionary<ToolType, StageAdvance>();

	/// <summary>
	/// This is what it should spawn if it goes to this stage, this will destroy the original component
	/// </summary>
	public GameObject FinalDeconstructedResult;

	public void CheckParts()
	{
		foreach (var Part in NeededParts) {
			if (Part.NumberNeeded == Part.NumberPresent)
			{
				MissingParts = false;
			}
			else
			{
				MissingParts = true;
				return;
			}
		
		}
	}
}
