﻿
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
	private Coroutine corHandler;


	private string InitialInfoText;

	private const string colorGreen = "008000",
						 colorGrey = "FFFFFF",
						 colorRed = "FF0000";

	private NetUIElement infoTimerDisplay;
	private NetUIElement InfoTimerDisplay
	{
		get
		{
			if (!infoTimerDisplay)
			{
				infoTimerDisplay = this["NukeTimerLabel"];
			}
			return infoTimerDisplay;
		}
	}

	private NetUIElement infoTimerColor;

	private NetUIElement InfoTimerColor
	{
		get
		{
			if (!infoTimerColor)
			{
				infoTimerColor = this["TimerNukeToggleColor"] as NetColorChanger;
			}
			return infoTimerColor;
		}
	}

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

	private NetUIElement infoNukeDisplay;
	private NetUIElement InfoNukeDisplay
	{
		get
		{
			if (!infoNukeDisplay)
			{
				infoNukeDisplay = this["NukeInfoDisplay"];
			}
			return infoNukeDisplay;
		}
	}
	private NetUIElement codeDisplay;

	private NetUIElement CodeDisplay
	{
		get
		{
			if (!codeDisplay)
			{
				codeDisplay = this["NukeCodeDisplay"];
			}
			return codeDisplay;
		}

	}

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

	public override void OnEnable()
	{
		base.OnEnable();
		if (CustomNetworkManager.Instance._isServer)
		{
			infoNukeDisplay = this["NukeInfoDisplay"];
			codeDisplay = this["NukeCodeDisplay"];
		}
	}

	protected override void InitServer()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			StartCoroutine(WaitForProvider());
		}
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		InfoTimerDisplay.SetValue = FormatTime(nuke.CurrentTimerSeconds);
		nuke.OnTimerUpdate.AddListener(timerSeconds => { InfoTimerDisplay.SetValue = FormatTime(timerSeconds); });

		Logger.Log(nameof(WaitForProvider), Category.NetUI);
	}

	private void Start()
	{
		//Not doing this for clients
		if (IsServer)
		{
			//	Logger.Log( $"{name} Kinda init. Nuke code is {NukeInteract.NukeCode}" );
			InitialInfoText = $"Enter {Nuke.NukeCode.ToString().Length}-digit code:";
			InfoNukeDisplay.SetValue = "Insert the disk!";
			if(!Nuke.IsAncharable)
			{
				InfoAnchorColor.SetValue = colorGrey;
			}
			
		}
	}

	#region Buttons

	public void DiskButton()
	{
		if (nuke.NukeSlot.IsEmpty)
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Empty!", "Insert the disk!"), ref corHandler);
			return;
		}
		Nuke.EjectDisk();
		InfoNukeDisplay.SetValue = "Insert the disk!";
		Clear();
	}

	public void SafetyToggle()
	{
		if (nuke.NukeSlot.IsEmpty)
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Insert the disk!", "Insert the disk!"), ref corHandler);
			return;
		}
		bool? isSafety = Nuke.SafetyNuke();
		if (isSafety != null)
		{
			InfoSafetyColor.SetValue = isSafety.Value ? colorGreen : colorRed;
			if (isSafety.Value) { InfoTimerColor.SetValue = colorRed; }
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Safety is: " + (isSafety.Value ? "On" : "Off")), ref corHandler);
		}
		else
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("No Access!", "Enter code:"), ref corHandler);
		}
	}

	public void AnchorNukeButton()
	{
		if(!Nuke.IsAncharable)
		{
			return;
		}
		if (nuke.NukeSlot.IsEmpty)
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Insert the disk!", "Insert the disk!"), ref corHandler);
			return;
		}
		bool? isAnchored = Nuke.AnchorNuke();
		if(isAnchored != null)
		{
			InfoAnchorColor.SetValue = isAnchored.Value ? colorRed : colorGreen ;
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Anchor is: " + (isAnchored.Value ? "Off" : "On")), ref corHandler);
		}
		else
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("No Access!", "Enter code:"), ref corHandler);
		}
	}

	public void TimerSetButton()
	{
		if (nuke.NukeSlot.IsEmpty)
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Insert the disk!", "Insert the disk!"), ref corHandler);
			return;
		}
		bool? isTimer = Nuke.ToggleTimer();
		if(isTimer != null)
		{
			this.TryStopCoroutine(ref corHandler);
			Clear();
			InfoTimerColor.SetValue = isTimer.Value ? colorGreen : colorRed;
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Timer is: " + (isTimer.Value ? "On" : "Off"), "Set the nuke."), ref corHandler);
		}
		else
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("No Access!", "Enter code:"), ref corHandler);
		}
	}

	public void EnterDigit(char digit)
	{
		if (nuke.NukeSlot.IsEmpty)
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Insert the disk!", "Insert the disk!"), ref corHandler);
			return;
		}
		if (Nuke.AppendKey(digit))
		{
			
			if (!Nuke.IsCodeRight)
			{
				int length = Nuke.CurrentCode.Length;
				//replace older digits with asterisks
				string newDigit = Nuke.CurrentCode.Substring(length <= 0 ? 0 : length - 1);
				CodeDisplay.SetValue = newDigit.PadLeft(length, '*');
				StartCoroutine(HideCode());
			}
			else
			{
				CodeDisplay.SetValue = Nuke.CurrentCode;
			}

		}
	}

	public void Clear()
	{
		Nuke.Clear();
		CodeDisplay.SetValue = "";
	}

	public void TryArm()
	{
		if (nuke.NukeSlot.IsEmpty)
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Insert the disk!", "Insert the disk!"), ref corHandler);
			return;
		}
		CodeAccess();

	}

	private void CodeAccess()
	{
		bool? isValid = Nuke.Validate();
		if(isValid == null)
		{
			return;
		}
		if (isValid.Value)
		{
			Clear();
			
			if (Nuke.IsTimer && Nuke.IsCodeRight)
			{
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Timer is set!"), ref corHandler);

			}
			else
			{
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Correct code!","Set the nuke."), ref corHandler);
			}

			
		}
		else
		{
			if (Nuke.IsTimer && Nuke.IsCodeRight)
			{
				Clear();
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Min 270 seconds!","Min 270 seconds!"), ref corHandler);
			}
			else
			{
				Clear();
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Incorrect code!", "Enter code:"), ref corHandler);
			}

		}
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}

	#endregion

	private string FormatTime(int timerSeconds)
	{
		if (!Nuke.IsTimer)
		{
			return string.Empty;
		}

		return TimeSpan.FromSeconds(timerSeconds).ToString("mm\\:ss");
	}

	private IEnumerator HideCode()
	{
		yield return WaitFor.Seconds(1);
		CodeDisplay.SetValue = "".PadLeft(CodeDisplay.Value.Length, '*');
	}

	private IEnumerator UpdateDisplay(string strBlink, string strSet = "")
	{
		InfoNukeDisplay.SetValue = strBlink;
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValue = "";
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValue = strBlink;
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValue = "";
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValue = strBlink;
		yield return WaitFor.Seconds(0.5f);

		InfoNukeDisplay.SetValue = strSet;
	}

}

