using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UnityEngine;

public class MutationChoiceElement : DynamicEntry
{

	public MutationSO MutationSO;

	public MutationUnlockMiniGame MutationUnlockMiniGame;

	public NetText_label NetText_label;

	public void SetValues(MutationSO InMutationSO, MutationUnlockMiniGame InMutationUnlockMiniGame)
	{
		MutationSO = InMutationSO;
		MutationUnlockMiniGame = InMutationUnlockMiniGame;
		NetText_label.SetValue($"Difficulty 100/{BodyPartMutations.GetMutationRoundData(InMutationSO).ResearchDifficult}");
	}

	public void OnSelect()
	{
		MutationUnlockMiniGame.GenerateForMutation(MutationSO);
	}
}
