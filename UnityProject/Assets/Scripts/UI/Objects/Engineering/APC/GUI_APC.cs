﻿using System.Collections;
using Objects.Engineering;
using UnityEngine;

namespace UI.Objects.Engineering
{
	public class GUI_APC : NetTab
	{
		/// <summary>
		/// The APC this tab is interacting with
		/// </summary>
		private APC LocalAPC;

		private float MaxCapacity = 0;

		/// <summary>
		/// Colours which will be used for foregrounds and backgrounds (in hex format)
		/// </summary>
		private static readonly Color
			fullBackground = DebugTools.HexToColor("82FF4C"),
			chargingBackground = DebugTools.HexToColor("A8B0F8"),
			criticalBackground = DebugTools.HexToColor("F86060"),
			fullForeground = DebugTools.HexToColor("00CC00"),
			chargingForeground = DebugTools.HexToColor("6070F8"),
			criticalForeground = DebugTools.HexToColor("F0F8A8");

		// Elements that we want to visually update:
		private NetColorChanger _backgroundColor;
		/// <summary>
		/// The text which is displaying the current state
		/// </summary>
		private NetColorChanger BackgroundColor {
			get {
				if (!_backgroundColor)
				{
					_backgroundColor = this["DisplayBG"] as NetColorChanger;
				}
				return _backgroundColor;
			}
		}

		private NetColorChanger _foregroundColors;

		private NetColorChanger _offOverlayColor;
		/// <summary>
		/// The text which is displaying the current state
		/// </summary>
		private NetColorChanger OffOverlayColor {
			get {
				if (!_offOverlayColor)
				{
					_offOverlayColor = this["OffOverlay"] as NetColorChanger;
				}
				return _offOverlayColor;
			}
		}
		private NetColorChanger _chargeFillColor;
		/// <summary>
		/// The text which is displaying the current state
		/// </summary>
		private NetColorChanger ChargeFillColor {
			get {
				if (!_chargeFillColor)
				{
					_chargeFillColor = this["Fill"] as NetColorChanger;
				}
				return _chargeFillColor;
			}
		}
		private NetLabel _statusText;
		/// <summary>
		/// The text which is displaying the current state
		/// </summary>
		private NetLabel StatusText {
			get {
				if (!_statusText)
				{
					_statusText = this["StatusText"] as NetLabel;
				}
				return _statusText;
			}
		}
		// private NetColorChanger _statusTextColor;
		// /// <summary>
		// /// Color of the status text
		// /// </summary>
		// private NetColorChanger StatusTextColor
		// {
		// 	get
		// 	{
		// 		if ( !_statusTextColor )
		// 		{
		// 			_statusTextColor = this["StatusText"] as NetColorChanger;
		// 		}
		// 		return _statusTextColor;
		// 	}
		// }

		private NetLabel _chargePercentage;
		/// <summary>
		/// The charge left in the APC
		/// </summary>
		private NetLabel ChargePercentage {
			get {
				if (!_chargePercentage)
				{
					_chargePercentage = this["ChargePercentage"] as NetLabel;
				}
				return _chargePercentage;
			}
		}

		// private NetColorChanger _chargePercentageColor;
		// /// <summary>
		// /// The color of the charge percentage
		// /// </summary>
		// private NetColorChanger ChargePercentageColor
		// {
		// 	get
		// 	{
		// 		if ( !_chargePercentageColor )
		// 		{
		// 			_chargePercentageColor = this["ChargePercentage"] as NetColorChanger;
		// 		}
		// 		return _chargePercentageColor;
		// 	}
		// }

		private NetLabel _electricalValues;
		/// <summary>
		/// The voltage, current and resistance measured by the APC
		/// </summary>
		private NetLabel ElectricalValues {
			get {
				if (!_electricalValues)
				{
					_electricalValues = this["ElectricalValues"] as NetLabel;
				}
				return _electricalValues;
			}
		}

		// private NetColorChanger _electricalValuesColor;
		// /// <summary>
		// /// The color of the values
		// /// </summary>
		// private NetColorChanger ElectricalValuesColor
		// {
		// 	get
		// 	{
		// 		if ( !_electricalValuesColor )
		// 		{
		// 			_electricalValuesColor = this["ElectricalValues"] as NetColorChanger;
		// 		}
		// 		return _electricalValuesColor;
		// 	}
		// }

		private NetColorChanger _electricalLabelsColor;
		/// <summary>
		/// The color of the voltage, current and resistance labels
		/// </summary>
		private NetColorChanger ElectricalLabelsColor {
			get {
				if (!_electricalLabelsColor)
				{
					_electricalLabelsColor = this["ElectricalLabels"] as NetColorChanger;
				}
				return _electricalLabelsColor;
			}
		}

		private NetSlider _chargeBar;
		/// <summary>
		/// APC charge bar
		/// </summary>
		private NetSlider ChargeBar {
			get {
				if (!_chargeBar)
				{
					_chargeBar = this["ChargeBar"] as NetSlider;
				}
				return _chargeBar;
			}
		}

		private void Start()
		{
			if (IsServer)
			{
				// Get the apc from the provider since it only works in start
				LocalAPC = Provider.GetComponent<APC>();
				CalculateMaxCapacity();
				StartRefresh();
			}
		}

		private void CalculateMaxCapacity()
		{
			float newCapacity = 0;
			foreach (DepartmentBattery battery in LocalAPC.ConnectedDepartmentBatteries)
			{
				newCapacity += battery.BatterySupplyingModule.CapacityMax;
			}
			MaxCapacity = newCapacity;
		}

		private string CalculateChargePercentage()
		{
			CalculateMaxCapacity();
			if (MaxCapacity == 0)
			{
				return "N/A";
			}

			float newCapacity = 0;
			foreach (DepartmentBattery battery in LocalAPC.ConnectedDepartmentBatteries)
			{
				newCapacity += battery.BatterySupplyingModule.CurrentCapacity;
			}

			return (newCapacity / MaxCapacity).ToString("P0");
		}

		// Functions for refreshing the display
		private bool RefreshDisplay = false;
		private void StartRefresh()
		{
			Logger.Log("Starting APC screen refresh", Category.Machines);
			RefreshDisplay = true;
			StartCoroutine(Refresh());
		}
		private void StopRefresh()
		{
			Logger.Log("Stopping APC screen refresh", Category.Machines);
			RefreshDisplay = false;
		}

		private IEnumerator Refresh()
		{
			UpdateScreenDisplay();
			yield return WaitFor.Seconds(0.5F);
			if (RefreshDisplay)
			{
				StartCoroutine(Refresh());
			}
		}
		private void UpdateScreenDisplay()
		{
			if (LocalAPC.State != APC.APCState.Dead)
			{
				OffOverlayColor.SetValueServer(Color.clear);
				Logger.LogTrace("Updating APC display", Category.Machines);
				// Display the electrical values using engineering notation
				string voltage = LocalAPC.Voltage.ToEngineering("V");
				string current = LocalAPC.Current.ToEngineering("A");
				string power = (LocalAPC.Voltage * LocalAPC.Current).ToEngineering("W");
				ElectricalValues.SetValueServer($"{voltage}\n{current}\n{power}");
				StatusText.SetValueServer(LocalAPC.State.ToString());
				ChargePercentage.SetValueServer(CalculateChargePercentage());
				// State specific updates
				switch (LocalAPC.State)
				{
					case APC.APCState.Full:
						BackgroundColor.SetValueServer(fullBackground);
						UpdateForegroundColours(fullForeground);
						ChargeBar.SetValueServer("100");
						break;
					case APC.APCState.Charging:
						BackgroundColor.SetValueServer(chargingBackground);
						UpdateForegroundColours(chargingForeground);
						AnimateChargeBar();
						break;
					case APC.APCState.Critical:
						BackgroundColor.SetValueServer(criticalBackground);
						UpdateForegroundColours(criticalForeground);
						ChargeBar.SetValueServer("0");
						break;
				}
			}
			else
			{
				BackgroundColor.SetValueServer(Color.clear); // Also changing the background since it bleeds through on the edges
				OffOverlayColor.SetValueServer(Color.black);
			}
		}

		private void UpdateForegroundColours(Color hexColor)
		{
			ElectricalLabelsColor.SetValueServer(hexColor);
			ChargeFillColor.SetValueServer(hexColor);
			// TODO These colors can't be updated until a solution for updating colors and text is figured out
			// ElectricalValuesColor.SetValue = hexColor;
			// StatusTextColor.SetValue = hexColor;
			// ChargePercentageColor.SetValue = hexColor;
		}
		private void AnimateChargeBar()
		{
			int chargeVal = int.Parse(ChargeBar.Value) + 10;
			// Update the charge bar animation
			ChargeBar.SetValueServer(chargeVal > 100 ? "0" : chargeVal.ToString());
		}
	}
}
