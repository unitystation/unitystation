using US13.Objects.Pipes.Devices;
using US13.ScriptableObjects.Atmospherics;
using US13.UI.Core.Net;
using US13.UI.Core.Net.Elements;
using US13.UI.Objects.Atmospherics.Canister;

namespace US13.UI.Objects.Atmospherics.Pipes
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
					((NetUIElement<string>)this[INFilter.Key]).MasterSetValue("1");
				}
				else
				{
					((NetUIElement<string>)this[INFilter.Key]).MasterSetValue("0");
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
			NetWheel.MasterSetValue(Filter.MaxPressure.ToString());
			numberSpinner.OnValueChange.AddListener(SetMaxPressure);
			PToggle.MasterSetValue(BoolToString(Filter.IsOn));
			SetFilteredGasValue(Filter.GasIndex);
		}

		public void SetFilteredGasValue(GasSO GasIndex)
		{
			foreach (var INFilter in Filter.CapableFiltering)
			{
				if (INFilter.Value == GasIndex) //Checks what button has been pressed  And sets the correct position appropriate
				{
					((NetUIElement<string>)this[INFilter.Key]).MasterSetValue("1");
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
