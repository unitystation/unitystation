using System;
using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UI.Objects.Medical;
using UnityEngine;
using Random = UnityEngine.Random;

public class MutationUnlockMiniGame : MonoBehaviour
{

	public GUI_DNAConsole GUI_DNAConsole;

	public BodyPartMutations.MutationRoundData.SliderMiniGameData  CurrentlySelected;

	public MutationMiniGameList MutationMiniGameList;

	public NetSlider Indicator;




	public void GenerateForMutation(MutationSO Mutation)
	{
		ClearSelection();
	}

	public void GenerateForSliderMiniGameData(BodyPartMutations.MutationRoundData.SliderMiniGameData SliderMiniGameData)
	{
		CurrentlySelected = SliderMiniGameData;
		foreach (var SlidingParameters in SliderMiniGameData.Parameters)
		{
			var Element = MutationMiniGameList.AddElement(SlidingParameters, this);
		}
	}


	public void Start()
	{
		if (!GUI_DNAConsole.IsMasterTab) return;
		UpdateIndicator();
		GenerateNewPuzzle();
	}


	public void ClearSelection()
	{
		CurrentlySelected = null;
		var Entries= MutationMiniGameList.Entries.ToArray();
		foreach (var Entrie in Entries)
		{
			MutationMiniGameList.MasterRemoveItem(Entrie);
		}
	}

	public void ServerTryUnlock()
	{

		if (CurrentlySelected == null) return;

		bool Satisfies = true;
		foreach (var Slide in MutationMiniGameList.Entries)
		{
			var OtherElement = Slide as MutationMiniGameElement;
			if (OtherElement.SatisfiesTarget() == false)
			{
				Satisfies = false;
			}

		}

		if (Satisfies)
		{
			GUI_DNAConsole.DNAConsole.CurrentDNACharge += MutationMiniGameList.Entries.Count;
			UpdateIndicator();
			GenerateNewPuzzle();
		}
	}

	public void GenerateNewPuzzle()
	{
		ClearSelection();
		var data = new BodyPartMutations.MutationRoundData.SliderMiniGameData();
		BodyPartMutations.MutationRoundData.PopulateSliderMiniGame(data, Random.Range(25, 66), false);
		GenerateForSliderMiniGameData(data);
	}

	public void UpdateIndicator()
	{
		var TargetValue = (float) Mathf.Min(GUI_DNAConsole.DNAConsole.CurrentDNACharge, GUI_DNAConsole.DNAConsole.RequiredDNASamples)  /
		                  (float) GUI_DNAConsole.DNAConsole.RequiredDNASamples;

		Indicator.SetValue(((int) (TargetValue *100)).ToString());
	}

	public void TryGenerateEgg()
	{
		if (GUI_DNAConsole.DNAConsole.CurrentDNACharge >= GUI_DNAConsole.DNAConsole.RequiredDNASamples)
		{
			GUI_DNAConsole.DNAConsole.CurrentDNACharge -= GUI_DNAConsole.DNAConsole.RequiredDNASamples;
			UpdateIndicator();
			GUI_DNAConsole.GenerateEgg();
		}
	}
}
