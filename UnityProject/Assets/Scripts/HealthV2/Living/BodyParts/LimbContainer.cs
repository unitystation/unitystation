using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

namespace HealthV2
{
	public class LimbContainer : BodyPartContainerBase
	{
		private List<ImplantLimb> limbs = new List<ImplantLimb>();

		[SerializeField]
		private PlayerMove playerMove;

		public override void ImplantAdded(Pickupable prevImplant, Pickupable newImplant)
		{
			base.ImplantAdded(prevImplant, newImplant);
			if (newImplant)
			{
				ImplantLimb newLimb = newImplant.GetComponent<ImplantLimb>();
				if (newLimb)
				{
					limbs.Add(newLimb);
				}
			}
			if (prevImplant)
			{
				ImplantLimb oldLimb = prevImplant.GetComponent<ImplantLimb>();
				if (oldLimb)
				{
					limbs.Remove(oldLimb);
				}
			}

			playerMove.UpdateSpeedFromLimbs();
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

		private void UpdatePlayerSpeed()
		{
			playerMove.ServerChangeSpeed(GetTotalRunningSpeed(), GetTotalWalkingSpeed());
		}
	}
}

