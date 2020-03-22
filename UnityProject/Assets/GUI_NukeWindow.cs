
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Optimized, new GUI_IDConsole
/// </summary>
public class GUI_NukeWindow : NetTab
{

	private const string colorGreen = "008000",
						colorGrey = "A9A9A9",
						 colorRed = "FF0000";

	private NetUIElement infoAnchorColor;
	private NetUIElement InfoAnchorColor
	{
		get
		{
			if (!infoAnchorColor)
			{
				infoAnchorColor = this["AnchorNukeToggleColor"] as NetColorChanger;
			}
			return infoAnchorColor;
		}
	}


	private NetUIElement infoSafetyColor;
	private NetUIElement InfoSafetyColor
	{
		get
		{
			if (!infoSafetyColor)
			{
				infoSafetyColor = this["SafetyNukeToggleColor"] as NetColorChanger;
			}
			return infoSafetyColor;
		}
	}

	private bool cooldown;

	private NetUIElement infoNukeDisplay;
	private NetUIElement InfoNukeDisplay
	{
		get
		{
			if (!infoNukeDisplay)
			{
				infoNukeDisplay = this["NukeInfoDisplay"] as NetLabel;
			}
			return infoNukeDisplay;
		}
	}

	private NetUIElement CodeDisplay => this["NukeCodeDisplay"];

	private string InitialInfoText;

	private Nuke nuke;
	private Nuke Nuke
	{
		get
		{
			if (!nuke)
			{
				nuke = Provider.GetComponent<Nuke>();
			}

			return nuke;
		}
	}

	[SerializeField]
	private NetPageSwitcher pageSwitcher = null;
	[SerializeField]
	private NetPage loginPage = null;
	[SerializeField]
	private NetPage mainPage = null;
	

	public override void OnEnable()
	{
		base.OnEnable();
		if (CustomNetworkManager.Instance._isServer)
		{
			
		}
	}


	public void ServerLogin()
	{
		if (!Nuke.NukeSlot.IsEmpty)
		{
			pageSwitcher.SetActivePage(mainPage);
		}
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}

	private void Start()
	{
		//Not doing this for clients
		if (IsServer)
		{
			//	Logger.Log( $"{name} Kinda init. Nuke code is {NukeInteract.NukeCode}" );
			InitialInfoText = $"Enter {Nuke.NukeCode.ToString().Length}-digit code:";
			InfoNukeDisplay.SetValue = InitialInfoText;
		}
	}

	public void DiskButton()
	{
		pageSwitcher.SetActivePage(loginPage);
		Nuke.EjectDisk();
		InfoSafetyColor.SetValue = colorGrey;
		InfoAnchorColor.SetValue = colorGrey;
	}

	public void SafetyToggle()
	{
		bool? isSafety = Nuke.SafetyNuke();
		if (isSafety != null)
		{
			InfoSafetyColor.SetValue = isSafety.Value ? colorGreen : colorRed;
		}
	}

	public void AnchorNukeButton()
	{
		bool? isAnchored = Nuke.AnchorNuke();
		if(isAnchored != null)
		{
			InfoAnchorColor.SetValue = isAnchored.Value ? colorGreen : colorRed;
		}
	}

	public void EnterDigit(char digit)
	{
		if (cooldown)
		{
			return;
		}
		DigitCode(digit);
	}

	private void DigitCode(char digit)
	{
		if (Nuke.AppendKey(digit))
		{
			int length = Nuke.CurrentCode.Length;
			//replace older digits with asterisks
			string newDigit = Nuke.CurrentCode.Substring(length <= 0 ? 0 : length - 1);
			CodeDisplay.SetValue = newDigit.PadLeft(length, '*');
			StartCoroutine(HideCode());
		}
	}

	private IEnumerator HideCode()
	{
		yield return WaitFor.Seconds(1);
		CodeDisplay.SetValue = "".PadLeft(CodeDisplay.Value.Length, '*');
	}

	public void Clear()
	{
		if (cooldown)
		{
			return;
		}
		Nuke.Clear();
		CodeDisplay.SetValue = "";
	}
	public void TryArm()
	{
		if (cooldown)
		{
			return;
		}
		CodeAccess();

	}
	private void CodeAccess()
	{
		if (Nuke.Validate())
		{
			InfoSafetyColor.SetValue = colorGreen;
			Clear();
		}
		else
		{
			StartCoroutine(ErrorCooldown());
		}
	}
	public IEnumerator ErrorCooldown()
	{
		cooldown = true;
		InfoNukeDisplay.SetValue = "Incorrect code!";
		yield return WaitFor.Seconds(0.5F);
		InfoNukeDisplay.SetValue = "";
		yield return WaitFor.Seconds(0.5F);
		InfoNukeDisplay.SetValue = "Incorrect code!";
		yield return WaitFor.Seconds(0.5F);
		InfoNukeDisplay.SetValue = "";
		yield return WaitFor.Seconds(0.5F);
		InfoNukeDisplay.SetValue = "Incorrect code!";
		yield return WaitFor.Seconds(0.5F);
		InfoNukeDisplay.SetValue = "";
		yield return WaitFor.Seconds(0.5F);
		cooldown = false;
		Clear();
		InfoNukeDisplay.SetValue = InitialInfoText;
	}


}

