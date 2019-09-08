using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_IdConsoleEntry : DynamicEntry
{
	[SerializeField]
	private NetLabel displayedName;
	[SerializeField]
	private NetColorChanger displayedBg;

	//This button is used in two types - as access and assignment
	private bool isAssignment;
	private JobOutfit jobOutfit;
	private Access access;
	private IdAccessCategory category;
	private IDCard idCard;
	private GUI_IdConsole console;

	public void SetUpAccess(GUI_IdConsole consoleToSet, IDCard idToSet, IdAccess accessToSet, IdAccessCategory categoryToSet)
	{
		isAssignment = false;
		console = consoleToSet;
		idCard = idToSet;
		access = accessToSet.RelatedAccess;
		category = categoryToSet;
		displayedName.SetValue = accessToSet.AccessName;
		CheckIsSet();
	}

	public void SetUpAssign(GUI_IdConsole consoleToSet, IDCard idToSet, JobOutfit jobToSet)
	{
		isAssignment = true;
		console = consoleToSet;
		idCard = idToSet;
		jobOutfit = jobToSet;
		displayedName.SetValue = jobOutfit.jobType.JobString();
		CheckIsSet();
	}

	private void SetButton(bool pressed)
	{
		displayedBg.SetValue = pressed ? "555555" : "ffffff";
		//Not sure if we will want to color code buttons
		/*
		if (isAssignment)
		{
			displayedBg.SetValue = pressed ? "999999" : "ffffff";
		}
		else
		{
			displayedBg.SetValue = pressed ? ColorUtility.ToHtmlStringRGB(category.CategoryPressedColor) : ColorUtility.ToHtmlStringRGB(category.CategoryColor);
		}
		*/
	}

	public void CheckIsSet()
	{
		if ((isAssignment && idCard.GetJobType == jobOutfit.jobType) ||
			(!isAssignment && idCard.accessSyncList.Contains((int)access)))
		{
			SetButton(true);
		}
		else
		{
			SetButton(false);
		}
	}

	public void PressButton()
	{
		if (isAssignment)
		{
			console.ChangeAssignment(jobOutfit);
		}
		else
		{
			console.ModifyAccess(access);
		}
		CheckIsSet();
	}
}
