using Antagonists;
using System.Collections.Generic;

public class ObjectiveInfo
{
	public string Description;
	public string Name;
	public bool Status;
	public bool IsCustom = false;
	public bool toDelete = false;
	public string ID = "";
	public short PrefabID = -1;
	public List<ObjectiveAttribute> Attributes = new List<ObjectiveAttribute>();

	public bool IsDifferent(CustomObjective objective)
	{
		return (objective.Description != Description || objective.Compleated != Status);
	}
}

public class AntagonistInfo
{
	public List<ObjectiveInfo> Objectives = new();
	public short antagID = -1;
	public bool IsAntagCanSeeObjectivesStatus;
}