using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Items.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace UI
{
	/// <summary>
	/// This script is designed to
	/// </summary>
	public class GUI_Explosive : NetTab
	{
		private Explosive explosiveDevice;

		[SerializeField] private NetLabel status;
		[SerializeField] private NetLabel timer;
		[SerializeField] private NetToggle modeToggleButton;
		[SerializeField] private NetToggle armToggleButton;

		[SerializeField] private Color safeColor = Color.green;
		[SerializeField] private Color dangerColor = Color.red;

		private float timerCount;

		private void Start()
		{
			StartCoroutine(WaitForProvider());
		}

		IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			explosiveDevice = Provider.GetComponent<Explosive>();
			if (explosiveDevice.DetonateImmediatelyOnSignal == false)
			{
				timer.Value = explosiveDevice.TimeToDetonate.ToString();
			}
			else
			{
				timer.Value = "Waiting signal..";
				status.Value = dangerColor.ToString();
			}

			modeToggleButton.Element.isOn = explosiveDevice.DetonateImmediatelyOnSignal;
			timerCount = explosiveDevice.TimeToDetonate;
			explosiveDevice.GUI = this;
		}

		public void ArmDevice()
		{
			explosiveDevice.IsArmed = armToggleButton.Element.isOn;
			if (modeToggleButton.Element.isOn == false && armToggleButton.Element.isOn == true)
			{
				modeToggleButton.enabled = false;
				armToggleButton.enabled = true;
				explosiveDevice.Countdown();
				UpdateStatusText();
				return;
			}
			UpdateStatusText();
		}

		public void ToggleMode()
		{
			explosiveDevice.ToggleMode(modeToggleButton.Element.isOn);
			UpdateStatusText();
		}

		public void IncreaseTimeByOne()
		{
			if(armToggleButton.Element.isOn) return;
			explosiveDevice.TimeToDetonate += 1;
			StartCoroutine(UpdateTimer());
		}
		public void IncreaseTimeByTen()
		{
			if(armToggleButton.Element.isOn) return;
			explosiveDevice.TimeToDetonate += 10;
			StartCoroutine(UpdateTimer());
		}
		public void DecreaseTimeByOne()
		{
			if(explosiveDevice.TimeToDetonate  - 1  < explosiveDevice.MinimumTimeToDetonate || armToggleButton.Element.isOn) return;
			explosiveDevice.TimeToDetonate -= 1;
			StartCoroutine(UpdateTimer());
		}
		public void DecreaseTimeByTen()
		{
			if(explosiveDevice.TimeToDetonate  - 10 < explosiveDevice.MinimumTimeToDetonate|| armToggleButton.Element.isOn) return;
			explosiveDevice.TimeToDetonate -= 10;
			StartCoroutine(UpdateTimer());
		}

		private void UpdateStatusText()
		{
			status.Value = explosiveDevice.IsArmed ? "C4 is armed" : "C4 is unarmed";
			timer.Value = explosiveDevice.DetonateImmediatelyOnSignal ? "Awaiting Signal" : DisplayTime();
			status.ElementTMP.color = explosiveDevice.IsArmed ? dangerColor : safeColor;
		}

		public IEnumerator UpdateTimer()
		{
			if (explosiveDevice.CountDownActive == false)
			{
				timerCount = explosiveDevice.TimeToDetonate;
				timer.Value = DisplayTime();
				yield break;
			}
			while (timerCount > 0)
			{
				timerCount -= 1;
				timer.Value = DisplayTime();
				yield return WaitFor.Seconds(1f);
			}
		}

		private string DisplayTime()
		{
			return $"{Mathf.RoundToInt(timerCount / 60)}:{(timerCount % 60).RoundToLargestInt()}";
		}
	}
}
