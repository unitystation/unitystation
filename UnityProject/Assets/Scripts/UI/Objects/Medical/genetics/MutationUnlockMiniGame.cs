using System;
using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UI.Objects.Medical;
using UnityEngine;

public class MutationUnlockMiniGame : MonoBehaviour
{

	public GUI_DNAConsole GUI_DNAConsole;

	public MutationSO CurrentlySelected;

	public MutationMiniGameList MutationMiniGameList;


	public MutationSO DUBUGMutation;

	public EmptyItemList MutationSelectionList;

	[NaughtyAttributes.Button()]
	public void DUBUGGenerateForMutation()
	{
		GenerateForMutation(DUBUGMutation);
	}

	public void GenerateForMutation(MutationSO Mutation)
	{
		ClearSelection();
		CurrentlySelected = Mutation;
		var data = BodyPartMutations.GetMutationRoundData(Mutation);

		foreach (var SlidingParameters in data.Parameters)
		{
			var Element = MutationMiniGameList.AddElement(SlidingParameters, this);

		}
	}

	public Dictionary<MutationSO, MutationChoiceElement> MutationToElement =
		new Dictionary<MutationSO, MutationChoiceElement>();

	public void Start()
	{
		if (GUI_DNAConsole.IsMasterTab)
		{
			foreach (var Mutation in GUI_DNAConsole.DNAConsole.ALLMutations)
			{
				var Element =  MutationSelectionList.AddItem() as MutationChoiceElement;
				Element.SetValues(Mutation, this);
				MutationToElement[Mutation] = Element;
			}

		}
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
			GUI_DNAConsole.AddMutation( CurrentlySelected);

			var Element = MutationToElement[CurrentlySelected];
			MutationSelectionList.MasterRemoveItem(Element);
			MutationToElement.Remove(CurrentlySelected);

			ClearSelection();

		}

	}
}
