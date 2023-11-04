using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
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
		private ExplosiveBase explosiveDevice;

		[SerializeField] private NetText_label status;
		[SerializeField] private NetText_label timer;
		[SerializeField] private NetToggle armToggleButton;
		[SerializeField] private NetToggle sbArmToggleButton;
		[SerializeField] private GameObject sbButtons;
		[SerializeField] private GameObject sharedButtons;
		[SerializeField] private GameObject sbDispalyLoc;
		[SerializeField] private GameObject sbTimerLoc;
		[SerializeField] private Image background;
		[SerializeField] private NetSpriteImage signalIcon;

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
			if(CustomNetworkManager.IsServer) signalIcon.MasterSetValue(explosiveDevice.DetonateImmediatelyOnSignal ? "0" : "1");
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			sbButtons.SetActive(false); //Disable this via code because NetUI doesn't register it from the prefab if it is disabled by default
			explosiveDevice = Provider.GetComponent<ExplosiveBase>();

			//Setup variables and graphics
			timerCount = explosiveDevice.TimeToDetonate;
			EnsureVisualsAreCorrect();
			UpdateStatusText();

			timerCount = explosiveDevice.TimeToDetonate;
			timer.MasterSetValue( DisplayTime());
			explosiveDevice.GUI = this;
		}

		public void ArmDevice()
		{
			PlaySoundsForPeepers(CommonSounds.Instance.Click01);
			if (explosiveDevice.IsArmed)
			{
				foreach (var peeper in Peepers)
				{
					Chat.AddExamineMsg(peeper.GameObject, $"<color=red>The {Provider.ExpensiveName()} is already armed!</color>");
					return;
				}
			}
			explosiveDevice.IsArmed = true;
			StartCoroutine(explosiveDevice.Countdown());
			UpdateStatusText();
		}

		public void ToggleMode()
		{
			PlaySoundsForPeepers(CommonSounds.Instance.Click01);
			if (explosiveDevice.IsArmed)
			{
				foreach (var peeper in Peepers)
				{
					Chat.AddExamineMsg(peeper.GameObject, $"<color=red>The {Provider.ExpensiveName()} is already armed!</color>");
					return;
				}
			}
			explosiveDevice.ToggleMode(!explosiveDevice.DetonateImmediatelyOnSignal);
			signalIcon.MasterSetValue(explosiveDevice.DetonateImmediatelyOnSignal ? "0" : "1");
			UpdateStatusText();
			foreach (var peeper in Peepers)
			{
				var signalStatus = explosiveDevice.DetonateImmediatelyOnSignal ? "awaits a signal" : "awaits armament";
				Chat.AddExamineMsg(peeper.GameObject, $"The {Provider.ExpensiveName()} {signalStatus}");
			}
		}

		public void IncreaseTimeByOne()
		{
			if(armToggleButton.Value == "1" || sbArmToggleButton.Value == "1") return;
			explosiveDevice.TimeToDetonate += 1;
			StartCoroutine(UpdateTimer());
			PlaySoundsForPeepers(CommonSounds.Instance.Click01);
		}
		public void IncreaseTimeByTen()
		{
			if(armToggleButton.Value == "1" || sbArmToggleButton.Value == "1") return;
			explosiveDevice.TimeToDetonate += 10;
			StartCoroutine(UpdateTimer());
			PlaySoundsForPeepers(CommonSounds.Instance.Click01);
		}
		public void DecreaseTimeByOne()
		{
			if (explosiveDevice.TimeToDetonate  - 1  < explosiveDevice.MinimumTimeToDetonate
			    || armToggleButton.Value == "1" || sbArmToggleButton.Value == "1") return;
			explosiveDevice.TimeToDetonate -= 1;
			StartCoroutine(UpdateTimer());
			PlaySoundsForPeepers(CommonSounds.Instance.Click01);
		}
		public void DecreaseTimeByTen()
		{
			if (explosiveDevice.TimeToDetonate  - 10 < explosiveDevice.MinimumTimeToDetonate
			    || armToggleButton.Value == "1" || sbArmToggleButton.Value == "1") return;
			explosiveDevice.TimeToDetonate -= 10;
			StartCoroutine(UpdateTimer());
			PlaySoundsForPeepers(CommonSounds.Instance.Click01);
		}

		private void UpdateStatusText()
		{
			if (explosiveDevice.DetonateImmediatelyOnSignal)
			{
				status.MasterSetValue("awaiting signal..");
				return;
			}
			status.MasterSetValue(explosiveDevice.IsArmed ? "Armed" : "Unarmed");
		}

		public IEnumerator UpdateTimer()
		{
			if (explosiveDevice.CountDownActive == false)
			{
				timerCount = explosiveDevice.TimeToDetonate;
				timer.MasterSetValue(DisplayTime());
				yield break;
			}
			while (timerCount > 0)
			{
				timerCount -= 1;
				timer.MasterSetValue(DisplayTime());
				yield return WaitFor.Seconds(1f);
			}
		}

		private string DisplayTime()
		{
			return $"{Mathf.RoundToInt(timerCount / 60)}:{(timerCount % 60).RoundToLargestInt()}";
		}
	}
}
