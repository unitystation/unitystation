using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class UI_Hands : UI_DynamicItemSlot
{
	public void SetUpHand(IDynamicItemSlotS bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		SetupSlot(bodyPartUISlots, StorageCharacteristics);
	}

}
