using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Hands : UI_DynamicItemSlot
{
	public void SetUpHand(IDynamicItemSlotS bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		SetupSlot(bodyPartUISlots, StorageCharacteristics);
	}

}
