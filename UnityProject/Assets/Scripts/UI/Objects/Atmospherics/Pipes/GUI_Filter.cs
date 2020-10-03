using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;

public class GUI_Filter : NetTab
{
	public string CurrentlyFiltering = "O2";
	public Pipes.Filter Filter;

	public NetWheel NetWheel;

	public NumberSpinner numberSpinner;

	public NetToggle PToggle;


	private Dictionary<string,Gas> CapableFiltering = new Dictionary<string,Gas>()
	{
		{"O2",Gas.Oxygen},
		{"N2",Gas.Nitrogen},
		{"PLS",Gas.Plasma},
		{"CO2",Gas.CarbonDioxide},
		{"NO2",Gas.NitrousOxide},
	};


	public void SetFilterAmount(string Number)
	{
		CurrentlyFiltering = Number;

		foreach (var INFilter in CapableFiltering)
		{
			if (INFilter.Key == Number.ToString()) //Checks what button has been pressed  And sets the correct position appropriate
			{
				((NetUIElement<string>) this[INFilter.Key]).SetValueServer("1");
			}
			else
			{
				((NetUIElement<string>) this[INFilter.Key]).SetValueServer("0");
			}
		}

		Filter.GasIndex = CapableFiltering[Number];
	}

	void Start()
	{
		if (Provider != null)
		{
			Filter = Provider.GetComponentInChildren<Pipes.Filter>();
		}
		numberSpinner.ServerSpinTo( Filter.ToMaxPressure);
		numberSpinner.DisplaySpinTo(Filter.ToMaxPressure);
		NetWheel.SetValueServer(Filter.ToMaxPressure.ToString());
		numberSpinner.OnValueChange.AddListener(SetMaxPressure);
		PToggle.SetValueServer(BOOLTOstring(Filter.IsOn)) ;
		((NetUIElement<string>) this["O2"]).SetValueServer("1");

	}

	public string BOOLTOstring(bool Bool)
	{
		if (Bool)
		{
			return "1";
		}
		else
		{
			return "0";
		}
	}

	public void TogglePower()
	{
		Filter.TogglePower();
	}


	public void SetMaxPressure(int To)
	{
		Filter.ToMaxPressure = To;
	}
}
