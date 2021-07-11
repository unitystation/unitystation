using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class DoubleHandController : MonoBehaviour
{

	public bool LeftHandActive;
	public GameObject LeftHand;
	public GameObject LeftHandOverlay;

	public bool RightHandActive;
	public GameObject RightHand;
	public GameObject RightHandOverlay;

	public GameObject Overly;

	public UI_Hands UI_LeftHand;
	public UI_Hands UI_RightHand;


	public HandsController RelatedHandsController;


	public void HideHands(bool HideState)
	{
		if (HideState)
		{
			LeftHand.SetActive(false);
			RightHand.SetActive(false);
		}
		else
		{
			if (RightHandActive)
			{
				RightHand.SetActive(true);
			}

			if (LeftHandActive)
			{
				LeftHand.SetActive(true);
			}
		}
	}


	public void AddHand(IDynamicItemSlotS bodyPartUISlots, BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		switch (StorageCharacteristics.namedSlot)
		{
			case NamedSlot.leftHand:
				LeftHand.SetActive(true);
				LeftHandActive = true;
				UI_LeftHand.SetUpHand(bodyPartUISlots, StorageCharacteristics);
				break;
			case NamedSlot.rightHand:
				RightHand.SetActive(true);
				RightHandActive = true;
				UI_RightHand.SetUpHand(bodyPartUISlots, StorageCharacteristics);
				break;
		}

		if (LeftHandActive && RightHandActive)
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
	public bool RemoveHand(
		BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		switch (StorageCharacteristics.namedSlot)
		{
			case NamedSlot.leftHand:
				if (RelatedHandsController.activeDoubleHandController == this
				    && RelatedHandsController.ActiveHand == NamedSlot.leftHand
				    && RightHandActive)
				{
					ActivateRightHand();
				}

				LeftHand.SetActive(false);
				LeftHandActive = false;
				UI_LeftHand.ReSetSlot();
				break;
			case NamedSlot.rightHand:

				if (RelatedHandsController.activeDoubleHandController == this
				    && RelatedHandsController.ActiveHand == NamedSlot.rightHand
				    && LeftHandActive)
				{
					ActivateLeftHand();
				}

				RightHand.SetActive(false);
				RightHandActive = false;
				UI_RightHand.ReSetSlot();
				break;
		}


		if (LeftHandActive == false && RightHandActive == false)
		{
			return true;
		}

		if ((LeftHandActive && RightHandActive) == false)
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
				return LeftHandActive ? UI_LeftHand : null;
			case NamedSlot.rightHand:
				return RightHandActive ? UI_RightHand : null;
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
		if (LeftHandOverlay.OrNull()?.activeSelf == false)
		{
			LeftHandOverlay.SetActive(true);
			RelatedHandsController.SetActiveHand(this, NamedSlot.leftHand);
		}
	}

	public void PickActiveHand()
	{
		if (RightHandActive)
		{
			RightHandOverlay.SetActive(true);
			RelatedHandsController.SetActiveHand(this, NamedSlot.rightHand);
		}
		else if (LeftHandActive)
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
		RightHandActive = false;
		LeftHandActive = false;
	}
}