
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

	private bool cooldown;

	private string InitialInfoText;

	private const string colorGreen = "008000",
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
			infoNukeDisplay = this["NukeInfoDisplay"];
			codeDisplay = this["NukeCodeDisplay"];
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
			InfoNukeDisplay.SetValue = InitialInfoText;
			
		}
	}

	public void DiskButton()
	{
		pageSwitcher.SetActivePage(loginPage);
		Nuke.EjectDisk();
		InfoNukeDisplay.SetValue = InitialInfoText;
		Clear();
	}

	public void SafetyToggle()
	{
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
			this.StartCoroutine(UpdateDisplay("No Access!"), ref corHandler);
		}
	}

	public void AnchorNukeButton()
	{
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
			this.StartCoroutine(UpdateDisplay("No Access!"), ref corHandler);
		}
	}

	public void TimerSetButton()
	{
		bool? isTimer = Nuke.ToggleTimer();
		if(isTimer != null)
		{
			this.TryStopCoroutine(ref corHandler);
			Clear();
			InfoNukeDisplay.SetValue = isTimer.Value ? "Set timer:" : InitialInfoText;
			InfoTimerColor.SetValue = isTimer.Value ? colorGreen : colorRed;
			//StartCoroutine(UpdateDisplay("Timer is: " + (isTimer.Value ? "On" : "Off")));
		}
		else
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("No Access!"), ref corHandler);
		}
	}

	private IEnumerator UpdateDisplay(string message)
	{
		InfoNukeDisplay.SetValue = message;
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValue = "";
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValue = message;
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValue = "";
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValue = message;
		yield return WaitFor.Seconds(0.5f);

		InfoNukeDisplay.SetValue = InitialInfoText;
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
		bool? isValid = Nuke.Validate();
		if(isValid == null)
		{
			return;
		}
		if (isValid.Value)
		{
			Clear();
			
			if (Nuke.IsTimer)
			{
				InfoNukeDisplay.SetValue = "Timer is set!";
			}
			else
			{
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Correct code!"), ref corHandler);
			}

			
		}
		else
		{
			if (!Nuke.IsTimer)
			{
				Clear();
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(ErrorCooldown() , ref corHandler);
			}
			else
			{
				Clear();
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(ErrorTimer() , ref corHandler);
			}

		}
	}

	public IEnumerator ErrorTimer()
	{
		InfoNukeDisplay.SetValue = "Min 270 seconds!";
		yield return WaitFor.Seconds(0.5F);
		InfoNukeDisplay.SetValue = "";
		yield return WaitFor.Seconds(0.5F);
		InfoNukeDisplay.SetValue = "Min 270 seconds!";
		yield return WaitFor.Seconds(0.5F);
		InfoNukeDisplay.SetValue = "";
		yield return WaitFor.Seconds(0.5F);
		InfoNukeDisplay.SetValue = "Min 270 seconds!";
		yield return WaitFor.Seconds(0.5F);
		InfoNukeDisplay.SetValue = "Set timer:";
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
		InfoNukeDisplay.SetValue = InitialInfoText;
	}

	private string FormatTime(int timerSeconds)
	{
		if (!Nuke.IsTimer)
		{
			return string.Empty;
		}

		return TimeSpan.FromSeconds(timerSeconds).ToString("mm\\:ss");
	}
}

