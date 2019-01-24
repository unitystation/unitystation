using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_APC : NetTab
{
	/// <summary>
	/// The APC this tab is interacting with
	/// </summary>
	private APC LocalAPC;

	private float MaxCapacity = 0;

	/// <summary>
	/// All of the colours which will be used for foregrounds and backgrounds
	/// </summary>
	private Color 	greenBackground = new Color(0.509804f, 1f, 0.2980392f),
					blueBackground = new Color(0.6588235f, 0.6901961f, 0.9725491f),
					redBackground = new Color(0.9725491f, 0.3764706f, 0.3764706f),
					greenForeground = new Color(0f, 0.8000001f, 0f),
					blueForeground = new Color(0.3764706f, 0.4392157f, 0.9725491f),
					redForeground = new Color(0.9411765f, 0.9725491f, 0.6588235f);

	[SerializeField]
	private GameObject ActiveDisplay;

	// Elements that we want to visually update:
	private NetSpriteImage _background;
	/// <summary>
	/// The text which is displaying the current state
	/// </summary>
	private NetSpriteImage Background
	{
		get
		{
			if ( !_background )
			{
				_background = this["DisplayBG"] as NetSpriteImage;
			}
			return _background;
		}
	}
	private NetSpriteImage _chargeFill;
	/// <summary>
	/// The text which is displaying the current state
	/// </summary>
	private NetSpriteImage ChargeFill
	{
		get
		{
			if ( !_chargeFill )
			{
				_chargeFill = this["Fill"] as NetSpriteImage;
			}
			return _chargeFill;
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

	private NetLabel _electricalLabels;
	/// <summary>
	/// The voltage, current and resistance labels
	/// </summary>
	private NetLabel ElectricalLabels
	{
		get
		{
			if ( !_electricalLabels )
			{
				_electricalLabels = this["ElectricalLabels"] as NetLabel;
			}
			return _electricalLabels;
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
		for (int i = 0; i < LocalAPC.ConnectedDepartmentBatteries.Count; i++)
		{
			newCapacity += LocalAPC.ConnectedDepartmentBatteries[i].CapacityMax;
		}
		MaxCapacity = newCapacity;
	}

	private string CalculateChargePercentage()
	{
		float newCapacity = 0;
		for (int i = 0; i < LocalAPC.ConnectedDepartmentBatteries.Count; i++)
		{
			newCapacity += LocalAPC.ConnectedDepartmentBatteries[i].CurrentCapacity;
		}

		if (MaxCapacity == 0)
		{
			return "0%";
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
		yield return new WaitForSeconds(0.5F);
		if (RefreshDisplay)
		{
			StartCoroutine( Refresh() );
		}
	}

	private void UpdateScreenDisplay()
	{
		if (LocalAPC.State != APC.APCState.Dead)
		{
			ActiveDisplay.SetActive(true);
			Logger.Log("Updating APC display", Category.NetUI);
			// Update the electrical values
			float voltage = LocalAPC.Voltage;
			float current = LocalAPC.Current;
			float resistance = LocalAPC.Resistance;
			float power = voltage * current;
			ElectricalValues.SetValue = $"{voltage:G6} V\n{current:G6} A\n{power:G6} W";
			StatusText.SetValue = LocalAPC.State.ToString();
			ChargePercentage.SetValue = CalculateChargePercentage();

			int chargeVal = int.Parse(ChargeBar.Value) + 10;
			// Update the charge bar animation
			if (chargeVal > 100)
			{
				ChargeBar.SetValue = "0";
			}
			else
			{
				ChargeBar.SetValue = chargeVal.ToString();
			}
			UpdateDisplayColours();
		}
		else
		{
			ActiveDisplay.SetActive(false);
		}
	}

	private void UpdateDisplayColours()
	{
		switch (LocalAPC.State)
		{
			case APC.APCState.Full:
				Background.Element.color = greenBackground;
				ElectricalLabels.Element.color = greenForeground;
				ElectricalValues.Element.color = greenForeground;
				StatusText.Element.color = greenForeground;
				ChargePercentage.Element.color = greenForeground;
				ChargeFill.Element.color = greenForeground;
				break;
			case APC.APCState.Charging:
				Background.Element.color = blueBackground;
				ElectricalLabels.Element.color = blueForeground;
				ElectricalValues.Element.color = blueForeground;
				StatusText.Element.color = blueForeground;
				ChargePercentage.Element.color = blueForeground;
				ChargeFill.Element.color = blueForeground;
				break;
			case APC.APCState.Critical:
				Background.Element.color = redBackground;
				ElectricalLabels.Element.color = redForeground;
				ElectricalValues.Element.color = redForeground;
				StatusText.Element.color = redForeground;
				ChargePercentage.Element.color = redForeground;
				ChargeFill.Element.color = redForeground;
				break;
		}
	}
}
