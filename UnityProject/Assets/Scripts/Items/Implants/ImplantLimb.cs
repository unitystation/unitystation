using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

namespace HealthV2
{
	public enum LimbType
	{
		LeftLeg,
		RightLeg,
		LeftArm,
		RightArm
	}
	public class ImplantLimb : ImplantBase
	{
		[SerializeField]
		[Tooltip("The walking speed that will be used when attached as a leg.\n" +
		         "Additive with any additional legs.\n" +
		         "Multiplied by leg efficiency.")]
		private float walkingSpeed = 1.5f;

		[SerializeField]
		[Tooltip("The running speed that will be used when attached as a leg.\n" +
		         "Additive with any additional legs.\n" +
		         "Multiplied by leg efficiency.")]
		private float runningSpeed = 3f;

		[SerializeField]
		[Tooltip("The crawling speed used for when the limb is attached as an arm.\n")]
		private float crawlingSpeed = 0.3f;

		[SerializeField]
		[Tooltip("A generalized number representing how efficient an arm this limb is.\n" +
		         "1 is a human hand.")]
		private float armEfficiency = 1f;

		[SerializeField]
		[Tooltip("A generalized number representing how efficient a leg this limb is.\n" +
		         "1 is a human leg.")]
		private float legEfficiency = 1f;

		[SerializeField]
		[Tooltip("Whether or not this limb can hold items.")]
		private bool canHoldItems = false;

		public float GetWalkingSpeed()
		{
			return walkingSpeed * legEfficiency;
		}

		public float GetRunningSpeed()
		{
			return runningSpeed * legEfficiency;
		}

		public float GetCrawlingSpeed()
		{
			return crawlingSpeed * armEfficiency;
		}


	}

}
