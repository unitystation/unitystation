using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Items.Implants.Organs;
using UnityEngine;

public class ToolCarousel : MonoBehaviour, IUIHandAreasSelectable
{
	public List<UI_DynamicItemSlot> AllSlots = new List<UI_DynamicItemSlot>();

	public List<GameObject> AllHighlightSlots = new List<GameObject>();

	public int ActiveHandInt = -1;

	public List<UI_DynamicItemSlot> AvailableSlots = new List<UI_DynamicItemSlot>();
	public List<UI_DynamicItemSlot> FilledSlots = new List<UI_DynamicItemSlot>();

	public HandsController RelatedHandsController;

	public void AddToCarousel(IDynamicItemSlotS bodyPartUISlots, BodyPartUISlots.StorageCharacteristics storageCharacteristics)
	{
		var slot = AvailableSlots[0];
		AvailableSlots.Remove(slot);
		slot.transform.parent.SetActive(true);
		slot.SetupSlot(bodyPartUISlots, storageCharacteristics);
		FilledSlots.Add(slot);
	}

	public void RemoveFromCarousel(BodyPartUISlots.StorageCharacteristics storageCharacteristics)
	{
		UI_DynamicItemSlot slot = null;
		foreach (var Filledslot in FilledSlots)
		{
			if (Filledslot._storageCharacteristics == storageCharacteristics)
			{
				slot = Filledslot;
				break;
			}
		}

		if (slot == null)
		{
			Logger.LogError($"Slot wasn't found for  {storageCharacteristics.namedSlot}");
			return;
		}

		FilledSlots.Remove(slot);
		slot.transform.parent.SetActive(false);
		slot.ReSetSlot();
		AvailableSlots.Add(slot);
	}


	public void SetActive(int index)
	{
		if (ActiveHandInt == index) return;

		AllHighlightSlots[index].SetActive(true);
		RelatedHandsController.SetActiveHand(this, AllSlots[index]._storageCharacteristics.namedSlot);
		ActiveHandInt = index;
	}

	public void DeSelect(NamedSlot Hand)
	{
		for (var index = 0; index < AllSlots.Count; index++)
		{
			var slot = AllSlots[index];
			if (slot._storageCharacteristics != null && slot._storageCharacteristics.namedSlot == Hand)
			{
				ActiveHandInt = -1;
				AllHighlightSlots[index].SetActive(false);
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

		SetActive(AllSlots.IndexOf(FilledSlots[newActiveHandInt]));
	}

	public UI_DynamicItemSlot GetHand(NamedSlot Hand)
	{
		foreach (var Filledslot in FilledSlots)
		{
			if (Filledslot._storageCharacteristics?.namedSlot == Hand)
			{
				return Filledslot;
			}
		}

		return null;
	}

	public bool HasFree()
	{
		if (AvailableSlots.Count > 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public bool HasSlot(BodyPartUISlots.StorageCharacteristics storageCharacteristics)
	{
		foreach (var Filledslot in FilledSlots)
		{
			if (Filledslot._storageCharacteristics == storageCharacteristics)
			{
				return true;

			}
		}
		return false;
	}

	public void HideAll()
	{
		foreach (var slot in AllSlots)
		{
			slot.transform.parent.SetActive(false);
		}
	}

}
