using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_Mixer  : NetTab
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
		Mixer.ToTakeFromInputTwo = 1-Number;

		ToTakeFromInputOne.SetValueServer(Mathf.RoundToInt(Number * 100f).ToString() + "%");
		ToTakeFromInputTwo.SetValueServer(Mathf.RoundToInt(Mixer.ToTakeFromInputTwo * 100f).ToString() + "%");
	}

	void Start()
	{
		if (Provider != null)
		{
			Mixer = Provider.GetComponentInChildren<Pipes.Mixer>();
		}
		numberSpinner.ServerSpinTo( Mixer.MaxPressure);
		numberSpinner.DisplaySpinTo(Mixer.MaxPressure);
		NetWheel.SetValueServer(Mixer.MaxPressure.ToString());
		numberSpinner.OnValueChange.AddListener(SetMaxPressure);
		PToggle.SetValueServer(BOOLTOstring(Mixer.IsOn)) ;

		ToTakeFromInputOne.SetValueServer(Mathf.RoundToInt(Mixer.ToTakeFromInputOne * 100f).ToString() + "%");
		ToTakeFromInputTwo.SetValueServer(Mathf.RoundToInt(Mixer.ToTakeFromInputTwo * 100f).ToString() + "%");
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
		Mixer.TogglePower();
	}


	public void SetMaxPressure(int To)
	{
		Mixer.MaxPressure = To;
	}
}
