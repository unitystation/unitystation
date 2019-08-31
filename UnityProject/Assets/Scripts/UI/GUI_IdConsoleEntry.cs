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
	private Assignment assignment;
	private Access access;
	private IDCard idCard;
	private GUI_IdConsole console;

	public void SetUp(GUI_IdConsole consoleToSet, IDCard idToSet, Access accessToSet, Assignment assignmentToSet, bool assignmentMode)
	{
		console = consoleToSet;
		idCard = idToSet;
		isAssignment = assignmentMode;
		if (isAssignment)
		{
			assignment = assignmentToSet;
			displayedName.SetValue = assignment.Job.ToString();
		}
		else
		{
			access = accessToSet;
			displayedName.SetValue = access.ToString();
		}
		CheckIsSet();
	}

	private void CheckIsSet()
	{
		if ((isAssignment && idCard.GetJobType == assignment.Job) ||
			(!isAssignment && idCard.accessSyncList.Contains((int)access)))
		{
			displayedBg.SetValue = "ffffff";
		}
		else
		{
			displayedBg.SetValue = "999999";
		}
	}

	public void PressButton()
	{
		if (isAssignment)
		{
			console.ChangeAssignment(assignment);
		}
		else
		{
			console.ModifyAccess(access);
		}
		CheckIsSet();
	}
}
