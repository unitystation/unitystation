﻿using UnityEngine;
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
		[SerializeField] private Toggle broadcastModeToggle;
		[SerializeField, Tooltip("1 = 100 channels")] private int numberOfChannels = 1;

		private SignalEmitter emittingDevice;
		private StationBouncedRadio stationBoundRadio;


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
			if (Provider.TryGetComponent<StationBouncedRadio>(out var radio))
			{
				stationBoundRadio = radio;
				broadcastModeToggle.isOn = radio.BroadcastToNearbyTiles;
				broadcastModeToggle.SetActive(true);
				yield break;
			}
			broadcastModeToggle.SetActive(false);
		}

		public void ChangeSliderFrequency()
		{
			if(emittingDevice == null) return;
			emittingDevice.Frequency = (float)Decimal.Round((decimal)frequencySlider.value, numberOfChannels);
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
				if(pick.ItemSlot?.ItemStorage.OrNull()?.Player == null) return;
				string status = emittingDevice.IsPowered ? "on" : "off";
				Chat.AddExamineMsg(pick.ItemSlot.ItemStorage.Player.gameObject, $"this device is now turned {status}");
			}
		}
		public void ToggleBroadcastMode()
		{
			stationBoundRadio.BroadcastToNearbyTiles = broadcastModeToggle.isOn;
			if (emittingDevice.gameObject.TryGetComponent<Pickupable>(out var pick))
			{
				if(pick.ItemSlot?.ItemStorage.OrNull()?.Player == null) return;
				string status = emittingDevice.IsPowered ? "broadcast messages to everyone nearby" : "broadcast messages for you only";
				Chat.AddExamineMsg(pick.ItemSlot.ItemStorage.Player.gameObject, $"this device will now {status}");
			}
		}
	}
}