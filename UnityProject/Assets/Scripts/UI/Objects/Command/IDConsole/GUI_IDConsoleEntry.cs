
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
	private bool isOccupation = false;
	[Tooltip("If assignment, occupation this button will grant.")]
	[SerializeField]
	private Occupation occupation = null;
	[Tooltip("If access, access this button will grant")]
	[SerializeField]
	private Access access = Access.maint_tunnels;

	[Tooltip("Color settings to apply when it's on")]
	[SerializeField]
	[Header("On Colors")]
	private ColorBlock onColors = ColorBlock.defaultColorBlock;

	[Tooltip("Color settings to use when it's off")]
	[SerializeField]
	[Header("Off Colors")]
	private ColorBlock offColors = ColorBlock.defaultColorBlock;

	//parent ID console tab this lives in
	private GUI_IDConsole console;
	private IDCard TargetCard => console.TargetCard;
	/// <summary>
	/// True if this entry is for an individual Access
	/// </summary>
	public bool IsAccess => !isOccupation;
	/// <summary>
	/// True if this entry is for an entire occupation
	/// </summary>
	public bool IsOccupation => isOccupation;

	/// <summary>
	/// If IsAccess, access this entry controls
	/// </summary>
	public Access Access => access;
	/// <summary>
	/// If IsOccupation, occupation this entry controls
	/// </summary>
	public Occupation Occupation => occupation;


	private Toggle toggle;
	private NetToggle netToggle;

	private void Awake()
	{
		console = GetComponentInParent<GUI_IDConsole>();
		toggle = GetComponentInChildren<Toggle>();
		netToggle = GetComponentInChildren<NetToggle>();
		//annoyingly, the built in Toggle has no way to just change color when it is selected, so we have
		//to add custom logic to do this
		toggle.onValueChanged.AddListener(OnToggleValueChanged);
		OnToggleValueChanged(toggle.isOn);
	}

	private void OnToggleValueChanged(bool isOn)
	{
		toggle.colors = isOn ? onColors : offColors;
		//occupations which are on are not clickable
		if (IsOccupation)
		{
			toggle.interactable = !isOn;
		}

	}

	public void ServerToggle(bool isToggled)
	{
		if (isOccupation)
		{
			if (isToggled)
			{
				console.ServerChangeAssignment(occupation);
			}
		}
		else if (!isOccupation)
		{
			console.ServerModifyAccess(access, isToggled);
		}
	}

	/// <summary>
	/// Refreshes the status of this entry based on the access / occupation of the target card
	/// </summary>
	public void ServerRefreshFromTargetCard()
	{
		//we check for current toggle status just to make sure we don't pointlessly send
		//a message when the value hasn't changed

		//no card inserted, nothing should be on
		if (TargetCard == null)
		{
			if (toggle.isOn)
			{
				netToggle.SetValueServer("0");
			}
			return;
		}

		if (isOccupation)
		{
			var hasOccupation = TargetCard.Occupation == occupation;
			if (hasOccupation && !toggle.isOn)
			{
				netToggle.SetValueServer("1");
			}
			else if (!hasOccupation && toggle.isOn)
			{
				netToggle.SetValueServer("0");
			}
		}
		else
		{
			var hasAccess = TargetCard.HasAccess(access);
			if (hasAccess && !toggle.isOn)
			{
				netToggle.SetValueServer("1");
			}
			else if (!hasAccess && toggle.isOn)
			{
				netToggle.SetValueServer("0");
			}
		}
	}
}
