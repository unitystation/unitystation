using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UnityEngine;

public class MutationUnlockSelectList : EmptyItemList
{
	public MutationMiniGameElement AddElement(BodyPartMutations.MutationRoundData.SliderParameters SliderParameters , MutationUnlockMiniGame MutationUnlockMiniGame) //data!!
	{
		var NewElement  = AddItem() as MutationMiniGameElement;
		NewElement.SetValues(SliderParameters, MutationUnlockMiniGame);
		return NewElement;
	}
}
