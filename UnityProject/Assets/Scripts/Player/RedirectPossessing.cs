using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

public class RedirectPossessing : MonoBehaviour
{
	public IPlayerPossessable IPlayerPossessable;

	public IPlayerPossessable ToPossessTo;

	public ItemStorage ItemStorage;

	public void Awake()
	{
		IPlayerPossessable = this.GetComponent<IPlayerPossessable>();
		IPlayerPossessable.OnPossessedBy.AddListener(Possessing);

		ItemStorage.ServerInventoryItemSlotSet += BrainInTransfer;

		if (ToPossessTo != null)
		{
			var Slots = ItemStorage.GetItemSlots();

			foreach (var Slot in Slots)
			{
				if (Slot.Item != null)
				{
					ToPossessTo = Slot.Item.GetComponent<IPlayerPossessable>();
					return;
				}
			}
		}
	}

	public void BrainInTransfer(Pickupable prevPart, Pickupable NewPart)
	{
		if (NewPart)
		{

			if (NewPart.GetComponentCustom<SaturationComponent>() != null)
			{
				NewPart.GetComponentCustom<SaturationComponent>().enabled = false;
			}

			if (NewPart.GetComponentCustom<HungerComponent>() != null)
			{
				NewPart.GetComponentCustom<HungerComponent>().enabled = false;
			}
			if (NewPart.GetComponentCustom<NaturalChemicalReleaseComponent>() != null)
			{
				NewPart.GetComponentCustom<NaturalChemicalReleaseComponent>().enabled = false;
			}



		}
		else if (prevPart)
		{
			if (NewPart.GetComponentCustom<SaturationComponent>() != null)
			{
				NewPart.GetComponentCustom<SaturationComponent>().enabled = true;
			}

			if (NewPart.GetComponentCustom<HungerComponent>() != null)
			{
				NewPart.GetComponentCustom<HungerComponent>().enabled = true;
			}
			if (NewPart.GetComponentCustom<NaturalChemicalReleaseComponent>() != null)
			{
				NewPart.GetComponentCustom<NaturalChemicalReleaseComponent>().enabled = true;
			}
		}
	}


	public void Possessing(Mind mind, IPlayerPossessable parent)
	{
		if (mind == null) return;
		if (parent != null) return;

		if (ToPossessTo == null)
		{
			var Slots = ItemStorage.GetItemSlots();

			foreach (var Slot in Slots)
			{
				if (Slot.Item != null)
				{
					ToPossessTo = Slot.Item.GetComponent<IPlayerPossessable>();
					break;
				}
			}
		}


		mind.SetPossessingObject(ToPossessTo.GameObject);
		mind.StopGhosting();
		return;
	}
}