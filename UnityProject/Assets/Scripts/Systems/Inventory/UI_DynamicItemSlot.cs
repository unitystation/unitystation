using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class UI_DynamicItemSlot : UI_ItemSlot
{
	public IDynamicItemSlotS RelatedBodyPartUISlots;

	//Used to set up sprites and properties of the Item slot
	public void SetupSlot(IDynamicItemSlotS bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics storageCharacteristics)
	{
		if (placeholderImage != null)placeholderImage.sprite = storageCharacteristics.placeholderSprite;
		namedSlot = storageCharacteristics.namedSlot;
		hoverName = storageCharacteristics.hoverName;
		RelatedBodyPartUISlots = bodyPartUISlots;
		var linkedSlot = ItemSlot.GetNamed(bodyPartUISlots.RelatedStorage, namedSlot);
		if (linkedSlot != null)
		{
			LinkSlot(linkedSlot);
		}
	}

	//Used to resit contents and visuals
	public void ReSetSlot()
	{
		RelatedBodyPartUISlots = null;
		UnLinkSlot();
	}
}
