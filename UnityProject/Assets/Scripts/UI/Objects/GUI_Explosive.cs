using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Items.Weapons;
using TMPro;
using UnityEngine;


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
		[SerializeField] private ToggleButton modeToggleButton;
		[SerializeField] private ToggleButton armToggleButton;

		[SerializeField] private Color safeColor = Color.green;
		[SerializeField] private Color dangerColor = Color.red;

		private float timerCount;

		private void Awake()
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
		}

		public void ArmDevice()
		{
			if (explosiveDevice.DetonateImmediatelyOnSignal && modeToggleButton.isOn)
			{
				status.Value = "C4 is armed.";
				status.Value = dangerColor.ToString();
				timer.Value = "Waiting signal..";
				return;
			}
			if (explosiveDevice.DetonateImmediatelyOnSignal && modeToggleButton.isOn == false)
			{
				status.Value = "C4 is unarmed.";
				status.Value = safeColor.ToString();
				UpdateTimer();
				return;
			}
			if (explosiveDevice.DetonateImmediatelyOnSignal == false && modeToggleButton.isOn == false)
			{
				status.Value = "C4 is armed.";
				status.Value = dangerColor.ToString();
				UpdateTimer();
				modeToggleButton.enabled = false;
				explosiveDevice.Countdown();
				return;
			}
			if (explosiveDevice.DetonateImmediatelyOnSignal == false && modeToggleButton.isOn == true)
			{
				status.Value = "C4 is unarmed.";
				status.Value = safeColor.ToString();
				UpdateTimer();
			}
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

		IEnumerator UpdateTimer()
		{
			if (explosiveDevice.DetonateImmediatelyOnSignal == false && modeToggleButton.isOn == false)
			{
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
