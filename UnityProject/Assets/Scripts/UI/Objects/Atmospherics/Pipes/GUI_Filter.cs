using System;
using System.Collections.Generic;
using ScriptableObjects.Atmospherics;
using UI.Core;
using Objects.Atmospherics;


namespace UI.Objects.Atmospherics
{
	public class GUI_Filter : NetTab
	{
		public Filter Filter;

		public NetWheel NetWheel;

		public NumberSpinner numberSpinner;

		public NetToggle PToggle;

		public void SetFilterAmount(string gasName)
		{
			foreach (var INFilter in Filter.CapableFiltering)
			{
				if (INFilter.Key == gasName) //Checks what button has been pressed  And sets the correct position appropriate
				{
					((NetUIElement<string>)this[INFilter.Key]).SetValueServer("1");
				}
				else
				{
					((NetUIElement<string>)this[INFilter.Key]).SetValueServer("0");
				}
			}

			Filter.GasIndex = Filter.CapableFiltering[gasName];
		}

		private void Start()
		{
			if (Provider != null)
			{
				Filter = Provider.GetComponentInChildren<Filter>();
			}
			numberSpinner.ServerSpinTo(Filter.MaxPressure);
			numberSpinner.DisplaySpinTo(Filter.MaxPressure);
			NetWheel.SetValueServer(Filter.MaxPressure.ToString());
			numberSpinner.OnValueChange.AddListener(SetMaxPressure);
			PToggle.SetValueServer(BoolToString(Filter.IsOn));
			SetFilteredGasValue(Filter.GasIndex);
		}

		public void SetFilteredGasValue(GasSO GasIndex)
		{
			foreach (var INFilter in Filter.CapableFiltering)
			{
				if (INFilter.Value == GasIndex) //Checks what button has been pressed  And sets the correct position appropriate
				{
					((NetUIElement<string>)this[INFilter.Key]).SetValueServer("1");
				}
			}
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

		public void TogglePower()
		{
			Filter.TogglePower();
		}

		public void SetMaxPressure(int value)
		{
			Filter.MaxPressure = value;
		}
	}
}
