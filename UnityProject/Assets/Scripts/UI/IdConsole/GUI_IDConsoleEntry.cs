
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// New ID console entry. Manages the logic of an individual button on the ID
/// console, which may be an assignment (like HOS, Miner, etc...) or an individual access (mining office, rnd lab, etc...)
/// </summary>
public class GUI_IDConsoleEntry : MonoBehaviour
{
	//This button is used in two types - as access and assignment
	[Tooltip("Whether this is an assignment (occupation) or an access (individual permission)")]
	[SerializeField]
	private bool isOccupation;
	[Tooltip("If assignment, occupation this button will grant.")]
	[SerializeField]
	private Occupation occupation;
	[Tooltip("If access, access this button will grant")]
	[SerializeField]
	private Access access;

	//parent ID console tab this lives in
	private GUI_IDConsole console;
	private IDCard TargetCard => console.TargetCard;
	private Toggle toggle;

	private void Awake()
	{
		console = GetComponentInParent<GUI_IDConsole>();
		toggle = GetComponentInChildren<Toggle>();
	}

	public void ServerToggle(bool isToggled)
	{
		if (isOccupation)
		{
			if (isToggled)
			{
				console.ChangeAssignment(occupation);
			}
			else
			{
				//can't untoggle it, you have to click something else
				//TODO: Find a better way to disable it when it is on so it can't be toggled off
				toggle.isOn = true;
			}
		}
		else if (!isOccupation)
		{
			console.ModifyAccess(access, isToggled);
		}
	}
}
