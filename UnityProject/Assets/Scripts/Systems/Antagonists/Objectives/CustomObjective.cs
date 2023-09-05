using Antagonists;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomObjective : Objective
{
	public bool Compleated => Complete;

	protected override bool CheckCompletion()
	{
		return Complete;
	}

	public void SetStatus(bool state)
	{
		Complete = state;
	}

	private void Init(string newDescription)
	{
		description = newDescription;
		name = newDescription;
		Complete = false;
	}

	public static CustomObjective Create(string newDescription)
	{
		var toRet = CreateInstance<CustomObjective>();
		toRet.Init(newDescription);
		return toRet;
	}

	public static CustomObjective Create(ObjectiveInfo objectiveInfo)
	{
		var toRet = CreateInstance<CustomObjective>();
		toRet.Init(objectiveInfo.Description);
		toRet.Complete = objectiveInfo.Status;
		return toRet;
	}

	public void Set(ObjectiveInfo custom)
	{
		Complete = custom.Status;
		description = custom.Description;
	}

	protected override void Setup()
	{
		
	}
}