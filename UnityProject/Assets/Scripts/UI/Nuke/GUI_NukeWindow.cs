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

	private static readonly Color
		colorGreen = DebugTools.HexToColor("00E100"),
		colorGrey = DebugTools.HexToColor("F3FEFF"),
		colorRed = DebugTools.HexToColor("FF2828");

	//get various ui elements (not the method i would choose, but it works)
	private NetUIElement<string> infoTimerDisplay;
	private NetUIElement<string> InfoTimerDisplay
	{
		get
		{
			if (!infoTimerDisplay)
			{
				infoTimerDisplay = (NetUIElement<string>)this["NukeTimerLabel"];
			}
			return infoTimerDisplay;
		}
	}

	private NetColorChanger infoTimerColor;

	private NetColorChanger InfoTimerColor
	{
		get
		{
			if (!infoTimerColor)
			{
				infoTimerColor = (NetColorChanger)this["TimerNukeToggleColor"];
			}
			return infoTimerColor;
		}
	}

	private NetColorChanger infoAnchorColor;
	private NetColorChanger InfoAnchorColor
	{
		get
		{
			if (!infoAnchorColor)
			{
				infoAnchorColor = (NetColorChanger)this["AnchorNukeToggleColor"];
			}
			return infoAnchorColor;
		}
	}


	private NetColorChanger infoSafetyColor;
	private NetColorChanger InfoSafetyColor
	{
		get
		{
			if (!infoSafetyColor)
			{
				infoSafetyColor = (NetColorChanger)this["SafetyNukeToggleColor"];
			}
			return infoSafetyColor;
		}
	}

	private NetUIElement<string> infoNukeDisplay;
	private NetUIElement<string> InfoNukeDisplay
	{
		get
		{
			if (!infoNukeDisplay)
			{
				infoNukeDisplay = (NetUIElement<string>)this["NukeInfoDisplay"];
			}
			return infoNukeDisplay;
		}
	}
	private NetUIElement<string> codeDisplay;

	private NetUIElement<string> CodeDisplay
	{
		get
		{
			if (!codeDisplay)
			{
				codeDisplay = (NetUIElement<string>)this["NukeCodeDisplay"];
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
			infoNukeDisplay = (NetUIElement<string>)this["NukeInfoDisplay"];
			codeDisplay = (NetUIElement<string>)this["NukeCodeDisplay"];
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

		InfoTimerDisplay.SetValueServer(FormatTime(nuke.CurrentTimerSeconds));
		nuke.OnTimerUpdate.AddListener(timerSeconds => { InfoTimerDisplay.SetValueServer(FormatTime(timerSeconds)); });

		Logger.Log(nameof(WaitForProvider), Category.NetUI);
	}

	private void Start()
	{
		//only executed on server
		if (IsServer)
		{
			//	Logger.Log( $"{name} Kinda init. Nuke code is {NukeInteract.NukeCode}" );
			InitialInfoText = $"Enter {Nuke.NukeCode.ToString().Length}-digit code:";
			InfoNukeDisplay.SetValueServer("Insert the disk!");
			if(!Nuke.IsAncharable)
			{
				InfoAnchorColor.SetValueServer(colorGrey);
			}

		}
	}

	#region Buttons

	/// <summary>
	/// Eject the nuke disk if it is containted within the nuke.
	/// </summary>
	public void DiskButton()
	{
		if (nuke.NukeSlot.IsEmpty)
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Empty!", "Insert the disk!"), ref corHandler);
			return;
		}
		Nuke.EjectDisk();
		InfoNukeDisplay.SetValueServer("Insert the disk!");
		Clear();
	}

	/// <summary>
	/// Toggle safety if the nuke disk has been inserted and the code has been entered
	/// </summary>
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
			InfoSafetyColor.SetValueServer(isSafety.Value ? colorGreen : colorRed);
			if (isSafety.Value) { InfoTimerColor.SetValueServer(colorRed); }
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Safety is: " + (isSafety.Value ? "On" : "Off"), (isSafety.Value ? "Nuke disarmed!" : "Nuke armed!")), ref corHandler);
		}
		else
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Enter the code first!", "Input code:"), ref corHandler);
		}
	}

	/// <summary>
	/// Toggle the nuke anchor if the disk has been inserted, the code has been input and the nuke can be anchored
	/// </summary>
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
			InfoAnchorColor.SetValueServer(isAnchored.Value ? colorRed : colorGreen) ;
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Anchor is: " + (isAnchored.Value ? "Off" : "On"), (isAnchored.Value ? "Nuke position Unlocked!" : "Nuke position Locked!")), ref corHandler);
		}
		else
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Safety is on!", "Nuke is unarmed!"), ref corHandler);
		}
	}

	/// <summary>
	/// Toggle the timer if the disk has been inserted
	/// </summary>
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
			InfoTimerColor.SetValueServer(isTimer.Value ? colorGreen : colorRed);
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Timer is: " + (isTimer.Value ? "On" : "Off"), (isTimer.Value ? "Set countdown timer:" : "Nuclear detonation aborted!")), ref corHandler);
			if (!isTimer.Value)
			{
				//Clear countdown timer upon disabling it
				InfoTimerDisplay.SetValueServer("");
			}
		}
		else
		{
			this.TryStopCoroutine(ref corHandler);
			this.StartCoroutine(UpdateDisplay("Safety is on!", "Nuke is unarmed!"), ref corHandler);
		}
	}

	/// <summary>
	/// Code input
	/// </summary>
	/// <param name="digit">The single digit to enter</param>
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
				CodeDisplay.SetValueServer(newDigit.PadLeft(length, '*'));
				StartCoroutine(HideCode());
			}
			else
			{
				CodeDisplay.SetValueServer(Nuke.CurrentCode);
			}

		}
	}

	public void Clear()
	{
		Nuke.Clear();
		CodeDisplay.SetValueServer("");
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
				this.StartCoroutine(UpdateDisplay("Timer has been set!", "Nuke detonation in:"), ref corHandler);

			}
			else
			{
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Correct code!","Access granted!"), ref corHandler);
			}


		}
		else
		{
			if (Nuke.IsTimer && Nuke.IsCodeRight)
			{
				Clear();
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Min 270 seconds!","Input time:"), ref corHandler);
			}
			else
			{
				Clear();
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Incorrect code!", "Input code:"), ref corHandler);
			}

		}
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
		CodeDisplay.SetValueServer("".PadLeft(((string)CodeDisplay.ValueObject).Length, '*'));
	}

	private IEnumerator UpdateDisplay(string strBlink, string strSet = "")
	{
		InfoNukeDisplay.SetValueServer(strBlink);
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValueServer("");
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValueServer(strBlink);
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValueServer("");
		yield return WaitFor.Seconds(0.5f);
		InfoNukeDisplay.SetValueServer(strBlink);
		yield return WaitFor.Seconds(0.5f);

		InfoNukeDisplay.SetValueServer(strSet);
	}
}
