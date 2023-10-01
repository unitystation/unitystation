using System.Collections;
using Logs;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Engineering;

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

		#region UI Elements

		/// <summary>
		/// The text which is displaying the current state
		/// </summary>
		private NetColorChanger BackgroundColor => _backgroundColor ??= this["DisplayBG"] as NetColorChanger;
		private NetColorChanger _backgroundColor;

		private NetColorChanger _foregroundColors;

		/// <summary>
		/// The text which is displaying the current state
		/// </summary>
		private NetColorChanger OffOverlayColor => _offOverlayColor ??= this["OffOverlay"] as NetColorChanger;
		private NetColorChanger _offOverlayColor;

		/// <summary>
		/// The text which is displaying the current state
		/// </summary>
		private NetColorChanger ChargeFillColor => _chargeFillColor ??= this["Fill"] as NetColorChanger;
		private NetColorChanger _chargeFillColor;

		/// <summary>
		/// The text which is displaying the current state
		/// </summary>
		private NetText_label StatusText => _statusText ??= this["StatusText"] as NetText_label;
		private NetText_label _statusText;

		/// <summary>
		/// The charge left in the APC
		/// </summary>
		private NetText_label ChargePercentage => _chargePercentage ??= this["ChargePercentage"] as NetText_label;
		private NetText_label _chargePercentage;

		/// <summary>
		/// The voltage, current and resistance measured by the APC
		/// </summary>
		private NetText_label ElectricalValues => _electricalValues ??= this["ElectricalValues"] as NetText_label;
		private NetText_label _electricalValues;

		/// <summary>
		/// The color of the voltage, current and resistance labels
		/// </summary>
		private NetColorChanger ElectricalLabelsColor => _electricalLabelsColor ??= this["ElectricalLabels"] as NetColorChanger;
		private NetColorChanger _electricalLabelsColor;

		/// <summary>
		/// APC charge bar
		/// </summary>
		private NetSlider ChargeBar => _chargeBar ??= this["ChargeBar"] as NetSlider;
		private NetSlider _chargeBar;

		#endregion

		private void Start()
		{
			if (IsMasterTab)
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
			Loggy.Log("Starting APC screen refresh", Category.Machines);
			RefreshDisplay = true;
			StartCoroutine(Refresh());
		}
		private void StopRefresh()
		{
			Loggy.Log("Stopping APC screen refresh", Category.Machines);
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
				OffOverlayColor.MasterSetValue(Color.clear);
				Loggy.LogTrace("Updating APC display", Category.Machines);
				// Display the electrical values using engineering notation
				string voltage = LocalAPC.Voltage.ToEngineering("V");
				string current = LocalAPC.Current.ToEngineering("A");
				string power = (LocalAPC.Voltage * LocalAPC.Current).ToEngineering("W");
				ElectricalValues.MasterSetValue($"{voltage}\n{current}\n{power}");
				StatusText.MasterSetValue(LocalAPC.State.ToString());
				ChargePercentage.MasterSetValue(CalculateChargePercentage());
				// State specific updates
				switch (LocalAPC.State)
				{
					case APC.APCState.Full:
						BackgroundColor.MasterSetValue(fullBackground);
						UpdateForegroundColours(fullForeground);
						ChargeBar.MasterSetValue("100");
						break;
					case APC.APCState.Charging:
						BackgroundColor.MasterSetValue(chargingBackground);
						UpdateForegroundColours(chargingForeground);
						AnimateChargeBar();
						break;
					case APC.APCState.Critical:
						BackgroundColor.MasterSetValue(criticalBackground);
						UpdateForegroundColours(criticalForeground);
						ChargeBar.MasterSetValue("0");
						break;
				}
			}
			else
			{
				BackgroundColor.MasterSetValue(Color.clear); // Also changing the background since it bleeds through on the edges
				OffOverlayColor.MasterSetValue(Color.black);
			}
		}

		private void UpdateForegroundColours(Color hexColor)
		{
			ElectricalLabelsColor.MasterSetValue(hexColor);
			ChargeFillColor.MasterSetValue(hexColor);
			// TODO These colors can't be updated until a solution for updating colors and text is figured out
			// ElectricalValuesColor.SetValue = hexColor;
			// StatusTextColor.SetValue = hexColor;
			// ChargePercentageColor.SetValue = hexColor;
		}
		private void AnimateChargeBar()
		{
			int chargeVal = int.Parse(ChargeBar.Value) + 10;
			// Update the charge bar animation
			ChargeBar.MasterSetValue(chargeVal > 100 ? "0" : chargeVal.ToString());
		}
	}
}
