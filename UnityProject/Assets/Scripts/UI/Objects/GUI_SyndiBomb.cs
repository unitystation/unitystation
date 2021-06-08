using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Items.Weapons;

namespace aaa
{
	public class GUI_SyndiBomb : NetTab
	{
		private SyndiBomb obj;

		[SerializeField] private NetLabel timerLabel = null;
		[SerializeField] private NetLabel freqLabel = null;
		[SerializeField] private NetSlider slider = null;
		[SerializeField] private Image switchobj;
		[SerializeField] private Sprite spriteleft;
		[SerializeField] private Sprite spriteright;
		[SerializeField] private NetPageSwitcher pageswitcher;

		

		public override void OnEnable()
		{
			base.OnEnable();
			StartCoroutine(WaitForProvider());
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Add(UpdateTimer, 1f);
			}
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateTimer);
			}
		}

		IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			obj = Provider.GetComponentInChildren<SyndiBomb>();
			UpdateFreq();
			UpdateTimer();
		}
		
		private void UpdateTimer()
		{
			var time = obj.timer;
			string min = Mathf.FloorToInt((time) / 60).ToString();
			string sec = ((time) % 60).ToString();
			sec = sec.Length >= 2 ? sec : "0" + sec;
			timerLabel.SetValueServer($"{min}:{sec}");
		}

		private void UpdateFreq()
		{
			int freq = obj.frequencyReceive;
			string hundred = ((freq - (freq % 100))/100).ToString();
			string tens = (freq % 100).ToString();
			tens = tens.Length >= 2 ? tens : "0" + tens;
			freqLabel.SetValueServer($"{hundred}.{tens}");
		}

		public void ToggleArm()
		{
			if (obj.armed == false && obj.timer < 5)
			{
				return;
			}
			else
			{
				obj.armed ^= true;
			}
		}

		public void AdjTimer(int ammount)
		{
			var newval = obj.timer + ammount;
			if(newval > 0)
			{
				obj.timer += ammount;
				UpdateTimer();
			}
			else
			{
				obj.timer = 0;
				UpdateTimer();
			}
		}

		public void AdjFreq(int ammount)
		{
			var newval = obj.frequencyReceive + ammount;
			if(newval > 0)
			{
				obj.UpdateFreq(newval);
				UpdateFreq();
			}
			else
			{
				obj.UpdateFreq(0);
				UpdateFreq();
			}
		}

		public void ClientSwitchSlider()
		{
			int value = int.Parse(slider.Value);
			ClientSwitch(value/100);
		}

		public void ClientSwitchPage()
		{
			int value = pageswitcher.Pages.IndexOf( pageswitcher.CurrentPage );
			ClientSwitch(value);
		}

		private void ClientSwitch(int value)
		{
			if (value == 0)
			{
				switchobj.overrideSprite = spriteleft;
			}
			if (value == 1)
			{
				switchobj.overrideSprite = spriteright;
			}
		}

		public void ServerSwitch()
		{
			int value = int.Parse(slider.Value);
			value = value/100;
			pageswitcher.SetActivePage(value);
			obj.freq = Convert.ToBoolean(value);
		}
	}
}