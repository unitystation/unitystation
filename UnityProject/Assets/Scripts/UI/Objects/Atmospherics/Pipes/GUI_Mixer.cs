﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI.Core;

namespace UI.Objects.Atmospherics
{
	public class GUI_Mixer : NetTab
	{
		public NetSlider Slider;

		public Pipes.Mixer Mixer;

		public NumberSpinner numberSpinner;

		public NetToggle PToggle;

		public NetWheel NetWheel;

		public NetLabel ToTakeFromInputOne;
		public NetLabel ToTakeFromInputTwo;

		public void Set()
		{
			float Number = (float.Parse(Slider.Value) / 100f);
			Mixer.ToTakeFromInputOne = Number;
			Mixer.ToTakeFromInputTwo = 1 - Number;

			ToTakeFromInputOne.SetValueServer(Mathf.RoundToInt(Number * 100f).ToString() + "%");
			ToTakeFromInputTwo.SetValueServer(Mathf.RoundToInt(Mixer.ToTakeFromInputTwo * 100f).ToString() + "%");
		}

		private void Start()
		{
			if (Provider != null)
			{
				Mixer = Provider.GetComponentInChildren<Pipes.Mixer>();
			}
			numberSpinner.ServerSpinTo(Mixer.MaxPressure);
			numberSpinner.DisplaySpinTo(Mixer.MaxPressure);
			NetWheel.SetValueServer(Mixer.MaxPressure.ToString());
			numberSpinner.OnValueChange.AddListener(SetMaxPressure);
			PToggle.SetValueServer(BoolToString(Mixer.IsOn));

			ToTakeFromInputOne.SetValueServer(Mathf.RoundToInt(Mixer.ToTakeFromInputOne * 100f).ToString() + "%");
			ToTakeFromInputTwo.SetValueServer(Mathf.RoundToInt(Mixer.ToTakeFromInputTwo * 100f).ToString() + "%");
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
