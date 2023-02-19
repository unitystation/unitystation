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

	public NetColorChanger NetColorChanger;

	public Dictionary<MutationMiniGameElement, float> AccumulatedForces = new Dictionary<MutationMiniGameElement, float>();

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
	}

	public void ValueChangeSlider(float UnusedValue)
	{

		if (containedInTab.IsMasterTab == false) return;

		var TargetFloat = SliderParameters.TargetPosition / 100f;


		var NearValue =Mathf.Abs( Mathf.Abs(Indicator.Element.value - TargetFloat ) - 1f);


		if (SatisfiesTarget())
		{
			NetColorChanger.MasterSetValue(Color.magenta);
		}
		else
		{
			NetColorChanger.MasterSetValue(Color.LerpUnclamped(Color.red, Color.green, NearValue));
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


	public void MainSliderChangeMaster(float value )
	{

		//Logger.LogWarning(" Target >  " +SliderParameters.TargetLever + "  Actual value >  " + Value.ToString()); //Useful for debugging
		AccumulatedForces[this] = value;


		RecalculateLine();
		foreach (var Slide in SliderParameters.Parameters)
		{
			var OtherElement = MutationUnlockMiniGame.MutationMiniGameList.Entries[Slide.Item2] as MutationMiniGameElement;
			OtherElement.AccumulatedForces[this] = value * Slide.Item1;
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
