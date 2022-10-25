using System.Collections.Generic;
using UnityEngine;
using UI.Core;
using UI.Core.NetUI;
using Objects.Atmospherics;

namespace UI.Objects.Atmospherics
{
	public class GUI_Mixer : NetTab
	{
		public NetSlider Slider;

		public Mixer Mixer;

		public NumberSpinner numberSpinner;

		public NetToggle PToggle;

		public NetWheel NetWheel;

		public NetText_label ToTakeFromInputOne;
		public NetText_label ToTakeFromInputTwo;

		public void Set()
		{
			float Number = (float.Parse(Slider.Value) / 100f);
			Mixer.ToTakeFromInputOne = Number;
			Mixer.ToTakeFromInputTwo = 1 - Number;

			ToTakeFromInputOne.MasterSetValue(Mathf.RoundToInt(Number * 100f).ToString() + "%");
			ToTakeFromInputTwo.MasterSetValue(Mathf.RoundToInt(Mixer.ToTakeFromInputTwo * 100f).ToString() + "%");
		}

		private void Start()
		{
			if (Provider != null)
			{
				Mixer = Provider.GetComponentInChildren<Mixer>();
			}
			numberSpinner.ServerSpinTo(Mixer.MaxPressure);
			numberSpinner.DisplaySpinTo(Mixer.MaxPressure);
			NetWheel.MasterSetValue(Mixer.MaxPressure.ToString());
			numberSpinner.OnValueChange.AddListener(SetMaxPressure);
			PToggle.MasterSetValue(BoolToString(Mixer.IsOn));

			ToTakeFromInputOne.MasterSetValue(Mathf.RoundToInt(Mixer.ToTakeFromInputOne * 100f).ToString() + "%");
			ToTakeFromInputTwo.MasterSetValue(Mathf.RoundToInt(Mixer.ToTakeFromInputTwo * 100f).ToString() + "%");
		}

		public string BoolToString(bool _bool)
		{
			return _bool ? "1" : "0";
		}

		public void TogglePower()
		{
			Mixer.TogglePower();
		}

		public void SetMaxPressure(int To)
		{
			Mixer.MaxPressure = To;
		}
	}
}
