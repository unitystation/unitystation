using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_DynamicItemSlot : UI_ItemSlot
{
	public IDynamicItemSlotS RelatedBodyPartUISlots;

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

	public void ReSetSlot()
	{
		RelatedBodyPartUISlots = null;
		UnLinkSlot();
	}
}
