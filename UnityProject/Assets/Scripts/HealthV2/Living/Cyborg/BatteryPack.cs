using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatteryPack : MonoBehaviour, IChargeable
{
	public List<Battery> Cells = new List<Battery>();

	public ItemStorage Storage;

	public bool FullyCharged()
	{
		bool FullyCharged = true;
		foreach (var Battery in Cells)
		{
			if (Battery.FullyCharged() == false)
			{
				FullyCharged = false;
				break;
			}
		}

		return FullyCharged;
	}



	public void ChargeBy(float Watts)
	{
		bool NonCharging = true;

		Battery ToCharge = null;

		//Code that charges Each battery individually until they're all full

		foreach (var Battery in Cells)
		{
			if (Battery.FullyCharged() == false)
			{
				ToCharge = Battery;
				break;
			}
		}

		if (ToCharge != null)
		{
			ToCharge.ChargeBy(Watts);
			return;
		}
		else
		{
			return;
		}

	}


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
