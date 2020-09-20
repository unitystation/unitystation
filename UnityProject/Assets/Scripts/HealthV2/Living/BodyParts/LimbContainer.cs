using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

namespace HealthV2
{
	public class LimbContainer : BodyPartContainerBase
	{
		private List<ImplantLimb> limbs = new List<ImplantLimb>();

		public event Action<ImplantLimb, ImplantLimb> LimbUpdateEvent;

		public override void ImplantAdded(Pickupable prevImplant, Pickupable newImplant)
		{
			base.ImplantAdded(prevImplant, newImplant);

			ImplantLimb newLimb = newImplant?.GetComponent<ImplantLimb>();
			ImplantLimb oldLimb = prevImplant?.GetComponent<ImplantLimb>();
			if (newImplant)
			{
				if (newLimb)
				{
					limbs.Add(newLimb);
				}
			}
			if (prevImplant)
			{
				if (oldLimb)
				{
					limbs.Remove(oldLimb);
				}
			}

			LimbUpdateEvent?.Invoke(oldLimb, newLimb);
		}

		public float GetTotalWalkingSpeed()
		{
			float totalSpeed = 0f;
			foreach (ImplantLimb limb in limbs)
			{
				totalSpeed += limb.GetWalkingSpeed();
			}

			return totalSpeed;
		}

		public float GetTotalRunningSpeed()
		{
			float totalSpeed = 0f;
			foreach (ImplantLimb limb in limbs)
			{
				totalSpeed += limb.GetRunningSpeed();
			}

			return totalSpeed;
		}

		public float GetTotalCrawlingSpeed()
		{
			float totalSpeed = 0f;
			foreach (ImplantLimb limb in limbs)
			{
				totalSpeed += limb.GetCrawlingSpeed();
			}

			return totalSpeed;
		}
	}
}

