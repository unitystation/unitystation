using System.Collections.Generic;
using Systems.Construction.Parts;
using UnityEngine;

namespace HealthV2.Living.Cyborg
{
	public class BatteryPack : MonoBehaviour, IChargeable
	{
		public List<Battery> Cells = new List<Battery>();

		public ItemStorage Storage;

		public bool IsFullyCharged
		{
			get
			{
				bool fullyCharged = true;
				foreach (var battery in Cells)
				{
					if (battery.IsFullyCharged == false)
					{
						fullyCharged = false;
						break;
					}
				}

				return fullyCharged;
			}
		}



		public void ChargeBy(float watts)
		{
			Battery toCharge = null;

			//Code that charges Each battery individually until they're all full

			foreach (var Battery in Cells)
			{
				if (Battery.IsFullyCharged == false)
				{
					toCharge = Battery;
					break;
				}
			}

			if (toCharge != null)
			{
				toCharge.ChargeBy(watts);
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
}
