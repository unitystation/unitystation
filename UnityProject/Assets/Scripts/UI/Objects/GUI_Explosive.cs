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
				timer.Value = (explosiveDevice.TimeToDetonate * 1000).ToString();
			}
			else
			{
				timer.Value = "Waiting signal..";
				status.Value = dangerColor.ToString();
			}

			modeToggleButton.Element.isOn = explosiveDevice.DetonateImmediatelyOnSignal;
			timerCount = explosiveDevice.TimeToDetonate;
		}

		public void ArmDevice()
		{
			explosiveDevice.IsArmed = armToggleButton.Element.isOn;
			if (explosiveDevice.DetonateImmediatelyOnSignal == false && modeToggleButton.Element.isOn == true)
			{
				modeToggleButton.enabled = false;
				explosiveDevice.Countdown();
				UpdateTimer();
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
			explosiveDevice.TimeToDetonate += 1;
			StartCoroutine(UpdateTimer());
		}
		public void IncreaseTimeByTen()
		{
			explosiveDevice.TimeToDetonate += 10;
			StartCoroutine(UpdateTimer());
		}
		public void DecreaseTimeByOne()
		{
			if(explosiveDevice.MinimumTimeToDetonate < timerCount - 1) return;
			explosiveDevice.TimeToDetonate -= 1;
			StartCoroutine(UpdateTimer());
		}
		public void DecreaseTimeByTen()
		{
			if(explosiveDevice.MinimumTimeToDetonate < timerCount - 10) return;
			explosiveDevice.TimeToDetonate -= 10;
			StartCoroutine(UpdateTimer());
		}

		private void UpdateStatusText()
		{
			status.Value = explosiveDevice.IsArmed ? "C4 is armed." : "C4 is unarmed.";
			timer.Value = explosiveDevice.DetonateImmediatelyOnSignal ? "Awaiting Signal" : (timerCount * 1000).ToString();
			status.Element.color = explosiveDevice.IsArmed ? dangerColor : safeColor;
		}

		IEnumerator UpdateTimer()
		{
			if (explosiveDevice.CountDownActive == false)
			{
				timerCount = explosiveDevice.TimeToDetonate;
				timer.Value = timerCount.ToString();
				yield break;
			}
			while (timerCount < explosiveDevice.TimeToDetonate * 1000)
			{
				timerCount -= 1;
				timer.Value = timerCount.ToString();
			}
		}
	}
}
