using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleHandController : MonoBehaviour
{
	public GameObject LeftHand;
	public GameObject LeftHandOverlay;

	public GameObject RightHand;
	public GameObject RightHandOverlay;

	public GameObject Overly;

	public UI_Hands UI_LeftHand;
	public UI_Hands UI_RightHand;


	public HandsController RelatedHandsController;

	public void AddHand(IDynamicItemSlotS bodyPartUISlots, BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		switch (StorageCharacteristics.namedSlot)
		{
			case NamedSlot.leftHand:
				LeftHand.SetActive(true);
				UI_LeftHand.SetUpHand(bodyPartUISlots, StorageCharacteristics);
				break;
			case NamedSlot.rightHand:
				RightHand.SetActive(true);
				UI_RightHand.SetUpHand(bodyPartUISlots, StorageCharacteristics);
				break;
		}

		if (LeftHand.activeSelf && RightHand.activeSelf)
		{
			Overly.SetActive(true);
		}
	}


	/// <summary>
	///
	/// </summary>
	/// <param name="bodyPartUISlots"></param>
	/// <param name="StorageCharacteristics"></param>
	/// <returns>To mark whether or not to destroy hand controller from no hands being Present in it</returns>
	public bool RemoveHand(IDynamicItemSlotS bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		switch (StorageCharacteristics.namedSlot)
		{
			case NamedSlot.leftHand:
				if (RelatedHandsController.activeDoubleHandController == this
				    && RelatedHandsController.ActiveHand == NamedSlot.leftHand
				    && RightHand.activeSelf)
				{
					ActivateRightHand();
				}

				LeftHand.SetActive(false);
				UI_LeftHand.ReSetSlot();
				break;
			case NamedSlot.rightHand:

				if (RelatedHandsController.activeDoubleHandController == this
				    && RelatedHandsController.ActiveHand == NamedSlot.rightHand
				    && LeftHand.activeSelf)
				{
					ActivateLeftHand();
				}

				RightHand.SetActive(false);
				UI_RightHand.ReSetSlot();
				break;
		}


		if (LeftHand.activeSelf == false && RightHand.activeSelf == false)
		{
			return true;
		}

		if ((LeftHand.activeSelf && RightHand.activeSelf) == false)
		{
			Overly.SetActive(false);
		}

		return false;
	}

	public UI_Hands GetHand(NamedSlot namedSlot)
	{
		switch (namedSlot)
		{
			case NamedSlot.leftHand:
				return LeftHand.activeSelf ? UI_LeftHand : null;
			case NamedSlot.rightHand:
				return RightHand.activeSelf ? UI_RightHand : null;
			default:
				return null;
		}
	}


	public void ActivateRightHand()
	{
		if (RightHandOverlay.activeSelf == false)
		{
			RightHandOverlay.SetActive(true);
			RelatedHandsController.SetActiveHand(this, NamedSlot.rightHand);
		}

	}

	public void ActivateLeftHand()
	{
		if (LeftHandOverlay.activeSelf == false)
		{
			LeftHandOverlay.SetActive(true);
			RelatedHandsController.SetActiveHand(this, NamedSlot.leftHand);
		}
	}

	public void PickActiveHand()
	{
		if (RightHand.activeSelf)
		{
			RightHandOverlay.SetActive(true);
			RelatedHandsController.SetActiveHand(this, NamedSlot.rightHand);
		}
		else if (LeftHand.activeSelf)
		{
			LeftHandOverlay.SetActive(true);
			RelatedHandsController.SetActiveHand(this, NamedSlot.leftHand);
		}
	}

	public void Deactivate(NamedSlot NamedSlot)
	{
		if (NamedSlot == NamedSlot.leftHand)
		{
			LeftHandOverlay.SetActive(false);
		}
		else
		{
			RightHandOverlay.SetActive(false);
		}
	}

	public void HideAll()
	{
		Overly.SetActive(false);
		RightHand.SetActive(false);
		LeftHand.SetActive(false);
		RightHandOverlay.SetActive(false);
		LeftHandOverlay.SetActive(false);
	}
}