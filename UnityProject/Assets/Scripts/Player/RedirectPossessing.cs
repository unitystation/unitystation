using System;
using System.Collections;
using System.Collections.Generic;
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
