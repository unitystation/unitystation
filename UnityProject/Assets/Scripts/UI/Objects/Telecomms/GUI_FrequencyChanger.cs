using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using Communications;
using Objects.Telecomms;
using System;

namespace UI.Objects.Telecomms
{
	public class GUI_FrequencyChanger : NetTab
	{
		[SerializeField] private TMP_Text freuquencyLabel;
		[SerializeField] private Slider frequencySlider;
		[SerializeField] private Toggle radioPowerToggle;

		private SignalEmitter emittingDevice;


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

			emittingDevice = Provider.GetComponent<SignalEmitter>();
			frequencySlider.minValue = emittingDevice.SignalData.MinMaxFrequancy.x;
			frequencySlider.maxValue = emittingDevice.SignalData.MinMaxFrequancy.y;
			UpdateFrequencyFromProvider();
			if(emittingDevice.RequiresPower == false) radioPowerToggle.SetActive(false);
			radioPowerToggle.isOn = emittingDevice.IsPowered;

		}

		public void ChangeFrequency(float freq)
		{
			emittingDevice.Frequency = freq;
			UpdateFrequencyFromProvider();
		}

		public void ChangeSliderFrequency()
		{
			if(emittingDevice == null) return;
			emittingDevice.Frequency = (float)Decimal.Round((decimal)frequencySlider.value, 1);
			UpdateFrequencyFromProvider();
		}

		private void UpdateFrequencyFromProvider()
		{
			freuquencyLabel.text = $"{emittingDevice.Frequency.ToString()}KHz";
		}

		public void ToggleDevicePower()
		{
			emittingDevice.IsPowered = radioPowerToggle.isOn;
			if (emittingDevice.gameObject.TryGetComponent<Pickupable>(out var pick))
			{
				if(pick.ItemSlot?.ItemStorage?.OrNull().Player == null) return;
				string status = emittingDevice.IsPowered ? "on" : "off";
				Chat.AddExamineMsg(pick.ItemSlot.ItemStorage.Player.gameObject, $"this device is now turned {status}");
			}
		}
	}
}