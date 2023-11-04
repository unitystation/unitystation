using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Command;

namespace UI.Objects.Command
{
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
		private NetUIElement<string> InfoTimerDisplay => infoTimerDisplay ??= (NetUIElement<string>)this["NukeTimerLabel"];

		private NetColorChanger infoTimerColor;

		private NetColorChanger InfoTimerColor => infoTimerColor ??= (NetColorChanger)this["TimerNukeToggleColor"];

		private NetColorChanger infoAnchorColor;
		private NetColorChanger InfoAnchorColor => infoAnchorColor ??= (NetColorChanger)this["AnchorNukeToggleColor"];

		private NetColorChanger infoSafetyColor;
		private NetColorChanger InfoSafetyColor => infoSafetyColor ??= (NetColorChanger)this["SafetyNukeToggleColor"];

		private NetUIElement<string> infoNukeDisplay;
		private NetUIElement<string> InfoNukeDisplay => infoNukeDisplay ??= (NetUIElement<string>)this["NukeInfoDisplay"];
		private NetUIElement<string> codeDisplay;

		private NetUIElement<string> CodeDisplay => codeDisplay ??= (NetUIElement<string>)this["NukeCodeDisplay"];

		private Nuke nuke;
		private Nuke Nuke => nuke ??= Provider.GetComponent<Nuke>();

		public override void OnEnable()
		{
			base.OnEnable();
			if (CustomNetworkManager.IsServer)
			{
				infoNukeDisplay = (NetUIElement<string>)this["NukeInfoDisplay"];
				codeDisplay = (NetUIElement<string>)this["NukeCodeDisplay"];
			}
		}

		protected override void InitServer()
		{
			if (CustomNetworkManager.IsServer)
			{
				StartCoroutine(WaitForProvider());
			}
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			InfoTimerDisplay.MasterSetValue(FormatTime(nuke.CurrentTimerSeconds));
			nuke.OnTimerUpdate.AddListener(timerSeconds => { InfoTimerDisplay.MasterSetValue(FormatTime(timerSeconds)); });

			Loggy.Log(nameof(WaitForProvider), Category.Machines);
		}

		private void Start()
		{
			if (IsMasterTab)
			{
				InitialInfoText = $"Enter {Nuke.NukeCode.ToString().Length}-digit code:";
				InfoNukeDisplay.MasterSetValue("Insert the disk!");
				if (!Nuke.IsAncharable)
				{
					InfoAnchorColor.MasterSetValue(colorGrey);
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
			InfoNukeDisplay.MasterSetValue("Insert the disk!");
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
				InfoSafetyColor.MasterSetValue(isSafety.Value ? colorGreen : colorRed);
				if (isSafety.Value) { InfoTimerColor.MasterSetValue(colorRed); }
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Safety is: " + (isSafety.Value ? "On" : "Off"), (isSafety.Value ? "Nuke is disarmed!" : "Nuke is armed!")), ref corHandler);
			}
			else
			{
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Enter code first!", "Input code:"), ref corHandler);
			}
		}

		/// <summary>
		/// Toggle the nuke anchor if the disk has been inserted, the code has been input and the nuke can be anchored
		/// </summary>
		public void AnchorNukeButton()
		{
			if (!Nuke.IsAncharable)
			{
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Cannot be unanchored!", "Cannot be unanchored!"), ref corHandler);
				return;
			}
			if (nuke.NukeSlot.IsEmpty)
			{
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Insert the disk!", "Insert the disk!"), ref corHandler);
				return;
			}
			bool? isAnchored = Nuke.AnchorNuke();
			if (isAnchored != null)
			{
				InfoAnchorColor.MasterSetValue(isAnchored.Value ? colorRed : colorGreen);
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Anchor is: " + (isAnchored.Value ? "Off" : "On"), (isAnchored.Value ? "Nuke position unlocked!" : "Nuke position locked!")), ref corHandler);
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
			if (isTimer != null)
			{
				this.TryStopCoroutine(ref corHandler);
				Clear();
				InfoTimerColor.MasterSetValue(isTimer.Value ? colorGreen : colorRed);
				this.TryStopCoroutine(ref corHandler);
				this.StartCoroutine(UpdateDisplay("Timer is: " + (isTimer.Value ? "On" : "Off"), (isTimer.Value ? "Set countdown timer:" : "Nuclear detonation aborted!")), ref corHandler);
				if (!isTimer.Value)
				{
					//Clear countdown timer upon disabling it
					InfoTimerDisplay.MasterSetValue("");
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
					CodeDisplay.MasterSetValue(newDigit.PadLeft(length, '*'));
					StartCoroutine(HideCode());
				}
				else
				{
					CodeDisplay.MasterSetValue(Nuke.CurrentCode);
				}

			}
		}

		public void Clear()
		{
			Nuke.Clear();
			CodeDisplay.MasterSetValue("");
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
			if (isValid == null)
			{
				return;
			}
			if (isValid.Value)
			{
				Clear();

				if (Nuke.IsTimer && Nuke.IsCodeRight)
				{
					this.TryStopCoroutine(ref corHandler);
					this.StartCoroutine(UpdateDisplay("Timer is set!", "Nuke detonation in:"), ref corHandler);

				}
				else
				{
					this.TryStopCoroutine(ref corHandler);
					this.StartCoroutine(UpdateDisplay("Correct code!", "Access granted!"), ref corHandler);
				}


			}
			else
			{
				if (Nuke.IsTimer && Nuke.IsCodeRight)
				{
					Clear();
					this.TryStopCoroutine(ref corHandler);
					this.StartCoroutine(UpdateDisplay("Min 270 seconds!", "Input time:"), ref corHandler);
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
			CodeDisplay.MasterSetValue("".PadLeft(((string)CodeDisplay.ValueObject).Length, '*'));
		}

		private IEnumerator UpdateDisplay(string strBlink, string strSet = "")
		{
			InfoNukeDisplay.MasterSetValue(strBlink);
			yield return WaitFor.Seconds(0.5f);
			InfoNukeDisplay.MasterSetValue("");
			yield return WaitFor.Seconds(0.5f);
			InfoNukeDisplay.MasterSetValue(strBlink);
			yield return WaitFor.Seconds(0.5f);
			InfoNukeDisplay.MasterSetValue("");
			yield return WaitFor.Seconds(0.5f);
			InfoNukeDisplay.MasterSetValue(strBlink);
			yield return WaitFor.Seconds(0.5f);

			InfoNukeDisplay.MasterSetValue(strSet);
		}
	}
}
