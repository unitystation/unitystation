using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Items.Implants.Organs;
using Logs;
using UnityEngine;

public class ToolCarousel : MonoBehaviour, IUIHandAreasSelectable
{
	public int ActiveHandInt = -1;

	public List<ToolCarouselSlot> FilledSlots = new List<ToolCarouselSlot>();

	public HandsController RelatedHandsController;

	public ToolCarouselSlot SlotPrefab;

	public GameObject wearables_bg;

	public void AddToCarousel(IDynamicItemSlotS bodyPartUISlots, BodyPartUISlots.StorageCharacteristics storageCharacteristics)
	{
		var slot = Instantiate(SlotPrefab, wearables_bg.transform);
		slot.transform.localScale = Vector3.one;
		slot.RelatedToolCarousel = this;
		slot.transform.SetActive(true);
		slot.RelatedUI_DynamicItemSlot.SetupSlot(bodyPartUISlots, storageCharacteristics);
		FilledSlots.Add(slot);
	}

	public void RemoveFromCarousel(BodyPartUISlots.StorageCharacteristics storageCharacteristics)
	{
		ToolCarouselSlot slot = null;
		foreach (var Filledslot in FilledSlots)
		{
			if (Filledslot.RelatedUI_DynamicItemSlot._storageCharacteristics == storageCharacteristics)
			{
				slot = Filledslot;
				break;
			}
		}

		if (slot == null)
		{
			Loggy.LogError($"Slot wasn't found for  {storageCharacteristics.namedSlot}");
			return;
		}

		FilledSlots.Remove(slot);
		Destroy(slot.gameObject);
	}


	public void SetActive(int index)
	{
		if (ActiveHandInt == index) return;

		FilledSlots[index].Highlight.SetActive(true);
		RelatedHandsController.SetActiveHand(this, FilledSlots[index].RelatedUI_DynamicItemSlot._storageCharacteristics.namedSlot);
		ActiveHandInt = index;


	}

	public void DeSelect(NamedSlot Hand)
	{
		for (var index = 0; index < FilledSlots.Count; index++)
		{
			var slot = FilledSlots[index];
			if (slot.RelatedUI_DynamicItemSlot._storageCharacteristics != null && slot.RelatedUI_DynamicItemSlot._storageCharacteristics.namedSlot == Hand)
			{
				ActiveHandInt = -1;
				FilledSlots[index].Highlight.SetActive(false);
			}
		}
	}

	public void SwapHand()
	{
		if (ActiveHandInt == -1) return;

		int newActiveHandInt = ActiveHandInt;

		newActiveHandInt++;
		if (newActiveHandInt >= FilledSlots.Count)
		{
			newActiveHandInt = 0;
		}

		if (FilledSlots.Count == 0)
		{
			ActiveHandInt = -1;
			return;
		}

		SetActive(FilledSlots.IndexOf(FilledSlots[newActiveHandInt]));
	}

	public UI_DynamicItemSlot GetHand(NamedSlot Hand)
	{
		foreach (var Filledslot in FilledSlots)
		{
			if (Filledslot.RelatedUI_DynamicItemSlot._storageCharacteristics?.namedSlot == Hand)
			{
				return Filledslot.RelatedUI_DynamicItemSlot;
			}
		}

		return null;
	}

	public bool HasFree()
	{
		return true;
	}

	public bool HasSlot(BodyPartUISlots.StorageCharacteristics storageCharacteristics)
	{
		foreach (var Filledslot in FilledSlots)
		{
			if (Filledslot.RelatedUI_DynamicItemSlot._storageCharacteristics == storageCharacteristics)
			{
				return true;
			}
		}
		return false;
	}

	public void HideAll()
	{
		foreach (var slot in FilledSlots)
		{
			slot.transform.parent.SetActive(false);
		}
	}

}
