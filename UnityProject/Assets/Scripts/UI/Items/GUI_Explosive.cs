using System;
using System.Collections;
using System.Collections.Generic;
using Items.Weapons;
using UnityEngine;
using UnityEngine.UI;
using UI.Core.NetUI;

namespace UI.Items
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
		[SerializeField] private NetToggle sbModeToggleButton;
		[SerializeField] private NetToggle armToggleButton;
		[SerializeField] private NetToggle sbArmToggleButton;
		[SerializeField] private GameObject sbButtons;
		[SerializeField] private GameObject sharedButtons;
		[SerializeField] private GameObject sbDispalyLoc;
		[SerializeField] private GameObject sbTimerLoc;
		[SerializeField] private Image background;

		[SerializeField] private Color safeColor = Color.green;
		[SerializeField] private Color dangerColor = Color.red;

		[SerializeField] private Sprite C4Graphic;
		[SerializeField] private Sprite X4Graphic;
		[SerializeField] private Sprite SBGraphic;

		private float timerCount;

		private void Start()
		{
			StartCoroutine(WaitForProvider());
		}

		public void EnsureVisualsAreCorrect()
		{
			if(explosiveDevice == null) return; //properly start() has not finished yet
			switch (explosiveDevice.ExplosiveType)
			{
				case ExplosiveType.C4:
					background.sprite = C4Graphic;
					break;
				case ExplosiveType.X4:
					background.sprite = X4Graphic;
					break;
				case ExplosiveType.SyndicateBomb:
					sbButtons.SetActive(true);
					sharedButtons.SetActive(false);
					status.transform.position = sbDispalyLoc.transform.position;
					timer.transform.position = sbTimerLoc.transform.position;
					background.sprite = SBGraphic;
					break;
			}
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			explosiveDevice = Provider.GetComponent<Explosive>();
			if (explosiveDevice.DetonateImmediatelyOnSignal == false)
			{
				timerCount = explosiveDevice.TimeToDetonate;
				DisplayTime();
			}
			else
			{
				timer.Value = "Waiting signal..";
				status.ElementTmp.color = dangerColor;
			}

			EnsureVisualsAreCorrect();

			modeToggleButton.Value = explosiveDevice.DetonateImmediatelyOnSignal ? "1" : "0";
			timerCount = explosiveDevice.TimeToDetonate;
			explosiveDevice.GUI = this;
		}

		public void ArmDevice()
		{
			if (explosiveDevice.ExplosiveType == ExplosiveType.SyndicateBomb)
			{
				explosiveDevice.IsArmed = sbArmToggleButton.Value == "1";
			}
			else
			{
				explosiveDevice.IsArmed = armToggleButton.Value == "1";
			}
			if ((modeToggleButton.Value == "0" || sbModeToggleButton.Value == "0")
			    && (armToggleButton.Value == "1" || sbArmToggleButton.Value == "1"))
			{
				modeToggleButton.enabled = false;
				sbModeToggleButton.enabled = false;
				armToggleButton.enabled = false;
				sbArmToggleButton.enabled = false;
				StartCoroutine(explosiveDevice.Countdown());
				UpdateStatusText();
				return;
			}
			UpdateStatusText();
		}

		public void ToggleMode()
		{
			if(modeToggleButton.Value == "1" || sbModeToggleButton.Value == "1")
			{
				explosiveDevice.ToggleMode(true);
			}
			else
			{
				explosiveDevice.ToggleMode(false);
			}
			UpdateStatusText();
		}

		public void IncreaseTimeByOne()
		{
			if(armToggleButton.Value == "1" || sbArmToggleButton.Value == "1") return;
			explosiveDevice.TimeToDetonate += 1;
			StartCoroutine(UpdateTimer());
		}
		public void IncreaseTimeByTen()
		{
			if(armToggleButton.Value == "1" || sbArmToggleButton.Value == "1") return;
			explosiveDevice.TimeToDetonate += 10;
			StartCoroutine(UpdateTimer());
		}
		public void DecreaseTimeByOne()
		{
			if (explosiveDevice.TimeToDetonate  - 1  < explosiveDevice.MinimumTimeToDetonate
			    || armToggleButton.Value == "1" || sbArmToggleButton.Value == "1") return;
			explosiveDevice.TimeToDetonate -= 1;
			StartCoroutine(UpdateTimer());
		}
		public void DecreaseTimeByTen()
		{
			if (explosiveDevice.TimeToDetonate  - 10 < explosiveDevice.MinimumTimeToDetonate
			    || armToggleButton.Value == "1" || sbArmToggleButton.Value == "1") return;
			explosiveDevice.TimeToDetonate -= 10;
			StartCoroutine(UpdateTimer());
		}

		private void UpdateStatusText()
		{
			status.Value = explosiveDevice.IsArmed ? "C4 is armed" : "C4 is unarmed";
			timer.Value = explosiveDevice.DetonateImmediatelyOnSignal ? "Awaiting Signal" : DisplayTime();
			status.ElementTmp.color = explosiveDevice.IsArmed ? dangerColor : safeColor;
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
