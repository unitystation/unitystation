using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class ConstructionStage 
{
	

	public Sprite StageSprite;
	public List<StageAdvance> StageAdvances;

	public List<ComponentData> NeededParts;

	public bool IncludePartsInitialisation;

	public ObjectState ObjectStateofStage;

	[HideInInspector]
	public bool MissingParts;

	[HideInInspector]
	public List<GameObject> PresentParts;

	public Dictionary<ToolType, StageAdvance> ToolStage = new Dictionary<ToolType, StageAdvance>();
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
