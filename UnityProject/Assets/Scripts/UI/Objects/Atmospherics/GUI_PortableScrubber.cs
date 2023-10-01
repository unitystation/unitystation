using System.Collections;
using System.Collections.Generic;
using Objects.Atmospherics;
using ScriptableObjects.Atmospherics;
using UI.Core.NetUI;
using UnityEngine;

public class GUI_PortableScrubber : NetTab
{
	public PortableScrubber PortableScrubber;
	public NetToggle PToggle;
	public void SetFilterType(string gasName)
	{
		foreach (var inFilter in Filter.CapableFiltering)
		{
			if (inFilter.Key == gasName) //Checks what button has been pressed  And sets the correct position appropriate
			{
				((NetUIElement<string>)this[inFilter.Key]).MasterSetValue("1");
			}
			else
			{
				((NetUIElement<string>)this[inFilter.Key]).MasterSetValue("0");
			}
		}

		PortableScrubber.TargetGas = Filter.CapableFiltering[gasName];
	}

	private void Start()
	{
		if (Provider != null)
		{
			PortableScrubber = Provider.GetComponentInChildren<PortableScrubber>();
		}

		PToggle.MasterSetValue(BoolToString(PortableScrubber.CurrentState));
		SetFilteredGasValue(PortableScrubber.TargetGas);
	}

	public string BoolToString(bool Bool)
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


	public void SetFilteredGasValue(GasSO GasIndex)
	{
		foreach (var INFilter in Filter.CapableFiltering) //TODO Probably better system if you want to make a custom list for scrubber make a separate thing
		{
			if (INFilter.Value == GasIndex) //Checks what button has been pressed  And sets the correct position appropriate
			{
				((NetUIElement<string>)this[INFilter.Key]).MasterSetValue("1");
			}
		}
	}

	public void TogglePower()
	{
		PortableScrubber.Toggle(PToggle.Element.isOn);
	}

}
