using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Core.NetUI;
using UnityEngine;
using UnityEngine.UI;


public class MutationMiniGameElement : DynamicEntry
{
	public  BodyPartMutations.MutationRoundData.SliderParameters SliderParameters;

	public MutationUnlockMiniGame MutationUnlockMiniGame;

	public NetServerSyncString netServerSyncString;

	public NetSlider SliderLever;

	public NetSlider Indicator;

	public Image LineImage;

	public bool locked = false;

	//net Service Synchronise string
	//Some hooks into the sliders to update values on client and server
	public void SetValues(BodyPartMutations.MutationRoundData.SliderParameters InSliderParameters , MutationUnlockMiniGame InMutationUnlockMiniGame)
	{
		SliderParameters = InSliderParameters;
		MutationUnlockMiniGame = InMutationUnlockMiniGame;
		netServerSyncString.SetValue(JsonConvert.SerializeObject(InSliderParameters));
		AccumulatedForces.Clear();
		locked = false;
	}

	public void Awake()
	{
		netServerSyncString.OnChange.AddListener(Setup);
		Indicator.GetComponent<Slider>().onValueChanged.AddListener(ValueChangeSlider);

		//PreviouslyValueLever = Indicator
	}

	public void ValueChangeSlider(float UnusedValue)
	{

		var TargetFloat = SliderParameters.TargetPosition / 100f;


		var NearValue =Mathf.Abs( Mathf.Abs(Indicator.Element.value - TargetFloat ) - 1f);


		if (SatisfiesTarget())
		{
			LineImage.color = Color.magenta;
		}
		else
		{
			LineImage.color = Color.LerpUnclamped(Color.red, Color.green, NearValue);
		}

	}

	public void Setup(string Data)
	{
		SliderParameters = JsonConvert.DeserializeObject<BodyPartMutations.MutationRoundData.SliderParameters>(Data);
		AccumulatedForces.Clear();
		locked = false;
	}

	public bool SatisfiesTarget()
	{
		var TargetFloat = SliderParameters.TargetPosition / 100f;


		var NearValue =Mathf.Abs( Mathf.Abs(Indicator.Element.value - TargetFloat ) - 1f);

		if (0.05f > Mathf.Abs((NearValue - 1f)))
		{
			return true;
		}

		return false;
	}



	public Dictionary<MutationMiniGameElement, float> AccumulatedForces =
		new Dictionary<MutationMiniGameElement, float>();

	public void MainSliderChangeMaster(float Value )
	{

		Logger.LogWarning(" Target >  " +SliderParameters.TargetLever + "  Actual value >  " + Value.ToString());
		AccumulatedForces[this] = Value;


		RecalculateLine();
		foreach (var Slide in SliderParameters.Parameters)
		{
			var OtherElement = MutationUnlockMiniGame.MutationMiniGameList.Entries[Slide.Item2] as MutationMiniGameElement;
			OtherElement.AccumulatedForces[this] = Value * Slide.Item1;
			OtherElement.RecalculateLine();
		}
	}

	public void RecalculateLine()
	{

		if (locked)
		{
			Indicator.SetValue(((int) (SliderParameters.TargetPosition)).ToString());
		}
		else
		{
			float totalForce = 0;
			foreach (var vForce in AccumulatedForces)
			{
				totalForce += vForce.Value;
			}

			Indicator.SetValue(((int) (totalForce *100)).ToString());
		}

	}

	public void LockThis()
	{
		if (locked) return;
		if (MutationUnlockMiniGame.GUI_DNAConsole.netCountdownTimer.Completed)
		{
			MutationUnlockMiniGame.GUI_DNAConsole.netCountdownTimer.StartCountdown(300);
			locked = true;
			RecalculateLine();
		}
	}
}
