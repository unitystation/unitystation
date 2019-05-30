﻿using System.Collections;
using UnityEngine;

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
	private const string fullBackground 	= "82FF4C",
						 chargingBackground = "A8B0F8",
						 criticalBackground = "F86060",
						 fullForeground 	= "00CC00",
						 chargingForeground = "6070F8",
						 criticalForeground = "F0F8A8";

	// Elements that we want to visually update:
	private NetColorChanger _backgroundColor;
	/// <summary>
	/// The text which is displaying the current state
	/// </summary>
	private NetColorChanger BackgroundColor
	{
		get
		{
			if ( !_backgroundColor )
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
	private NetColorChanger OffOverlayColor
	{
		get
		{
			if ( !_offOverlayColor )
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
	private NetColorChanger ChargeFillColor
	{
		get
		{
			if ( !_chargeFillColor )
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
	private NetLabel StatusText
	{
		get
		{
			if ( !_statusText )
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
	private NetLabel ChargePercentage
	{
		get
		{
			if ( !_chargePercentage )
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
	private NetLabel ElectricalValues
	{
		get
		{
			if ( !_electricalValues )
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
	private NetColorChanger ElectricalLabelsColor
	{
		get
		{
			if ( !_electricalLabelsColor )
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
	private NetSlider ChargeBar
	{
		get
		{
			if ( !_chargeBar )
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
		CalculateMaxCapacity ();
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
		Logger.Log("Starting APC screen refresh", Category.NetUI);
		RefreshDisplay = true;
		StartCoroutine( Refresh() );
	}
	private void StopRefresh()
	{
		Logger.Log("Stopping APC screen refresh", Category.NetUI);
		RefreshDisplay = false;
	}

	private IEnumerator Refresh()
	{
		UpdateScreenDisplay();
		yield return WaitFor.Seconds(0.5F);
		if (RefreshDisplay)
		{
			StartCoroutine( Refresh() );
		}
	}
	private void UpdateScreenDisplay()
	{
		if (LocalAPC.State != APC.APCState.Dead)
		{
			OffOverlayColor.SetValue = DebugTools.ColorToHex(Color.clear);
			Logger.LogTrace("Updating APC display", Category.NetUI);
			// Display the electrical values using engineering notation
			string voltage = LocalAPC.Voltage.ToEngineering("V");
			string current = LocalAPC.Current.ToEngineering("A");
			string power = (LocalAPC.Voltage * LocalAPC.Current).ToEngineering("W");
			ElectricalValues.SetValue = $"{voltage}\n{current}\n{power}";
			StatusText.SetValue = LocalAPC.State.ToString();
			ChargePercentage.SetValue = CalculateChargePercentage();
			// State specific updates
			switch (LocalAPC.State)
			{
				case APC.APCState.Full:
					BackgroundColor.SetValue = fullBackground;
					UpdateForegroundColours(fullForeground);
					ChargeBar.SetValue = "100";
					break;
				case APC.APCState.Charging:
					BackgroundColor.SetValue = chargingBackground;
					UpdateForegroundColours(chargingForeground);
					AnimateChargeBar();
					break;
				case APC.APCState.Critical:
					BackgroundColor.SetValue = criticalBackground;
					UpdateForegroundColours(criticalForeground);
					ChargeBar.SetValue = "0";
					break;
			}
		}
		else
		{
			BackgroundColor.SetValue = DebugTools.ColorToHex(Color.clear); // Also changing the background since it bleeds through on the edges
			OffOverlayColor.SetValue = DebugTools.ColorToHex(Color.black);
		}
	}

	private void UpdateForegroundColours(string hexColor)
	{
		ElectricalLabelsColor.SetValue = hexColor;
		ChargeFillColor.SetValue = hexColor;
		// TODO These colors can't be updated until a solution for updating colors and text is figured out
		// ElectricalValuesColor.SetValue = hexColor;
		// StatusTextColor.SetValue = hexColor;
		// ChargePercentageColor.SetValue = hexColor;
	}
	private void AnimateChargeBar()
	{
		int chargeVal = int.Parse(ChargeBar.Value) + 10;
		// Update the charge bar animation
		ChargeBar.SetValue = chargeVal > 100 ? "0" : chargeVal.ToString();
	}
}
