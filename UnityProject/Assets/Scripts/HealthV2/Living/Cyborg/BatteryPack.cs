using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatteryPack : MonoBehaviour
{
	public List<Battery> Cells = new List<Battery>();

	public ItemStorage Storage;


	public void Awake()
	{
		Storage.ServerInventoryItemSlotSet += BodyPartTransfer;
	}

	private void BodyPartTransfer(Pickupable prevImplant, Pickupable newImplant)
	{
		Battery Battery = null;
		if (newImplant && newImplant.TryGetComponent(out Battery))
		{
			if (Cells.Contains(Battery) == false)
			{
				Cells.Add(Battery);
			}
		}

		if (prevImplant && prevImplant.TryGetComponent(out Battery))
		{
			if (Cells.Contains(Battery))
			{
				Cells.Remove(Battery);
			}
		}
	}
}
