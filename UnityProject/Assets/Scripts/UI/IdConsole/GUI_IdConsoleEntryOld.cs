using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Old ID entry component, used only for stress testing nettab system.
/// </summary>
public class GUI_IdConsoleEntryOld : DynamicEntry
{
	[SerializeField]
	private NetLabel displayedName = null;
	[SerializeField]
	private NetColorChanger displayedBg = null;

	//This button is used in two types - as access and assignment
	private bool isAssignment;
	private Occupation occupation;
	private Access access;
	private IdAccessCategory category;
	private IDCard idCard;
	private GUI_IdConsoleOld console;

	public void SetUpAccess(GUI_IdConsoleOld consoleToSet, IDCard idToSet, IdAccess accessToSet, IdAccessCategory categoryToSet)
	{
		isAssignment = false;
		console = consoleToSet;
		idCard = idToSet;
		access = accessToSet.RelatedAccess;
		category = categoryToSet;
		displayedName.SetValueServer(accessToSet.AccessName);
		CheckIsSet();
	}

	public void SetUpAssign(GUI_IdConsoleOld consoleOldToSet, IDCard idToSet, Occupation occupationToSet)
	{
		isAssignment = true;
		console = consoleOldToSet;
		idCard = idToSet;
		occupation = occupationToSet;
		displayedName.SetValueServer(occupationToSet.JobType.JobString());
		CheckIsSet();
	}

	private void SetButton(bool pressed)
	{
		displayedBg.SetValueServer(pressed ? DebugTools.HexToColor("555555") : Color.white);
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
		if ((isAssignment && idCard.JobType == occupation.JobType) ||
			(!isAssignment && idCard.HasAccess(access)))
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
			console.ChangeAssignment(occupation);
		}
		else
		{
			console.ModifyAccess(access);
		}
		CheckIsSet();
	}
}
