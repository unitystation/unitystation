using System;
using HealthV2;
using Player.Movement;
using UnityEngine;

namespace HealthV2
{
	public class Limb : BodyPartFunctionality, IMovementEffect
	{
		[SerializeField]
		[Tooltip("The walking speed that will be used when attached as a leg.\n" +
		         "Additive with any additional legs.\n" +
		         "Multiplied by leg efficiency.")]
		private float walkingSpeed = 1.5f;
		public float WalkingSpeed => walkingSpeed;

		[SerializeField]
		[Tooltip("The running speed that will be used when attached as a leg.\n" +
		         "Additive with any additional legs.\n" +
		         "Multiplied by leg efficiency.")]
		private float runningSpeed = 3f;
		public float RunningSpeed => runningSpeed;

		[SerializeField] [Tooltip("The crawling speed used for when the limb is attached as an arm.\n")]
		private float crawlingSpeed = 0.3f;
		public float CrawlingSpeed => crawlingSpeed;

		[SerializeField]
		[Tooltip("A generalized number representing how efficient an arm this limb is.\n" +
		         "1 is a human hand.")]
		private float armEfficiency = 1f;
		public float ArmEfficiency => armEfficiency;

		[SerializeField]
		[Tooltip("A generalized number representing how efficient a leg this limb is.\n" +
		         "1 is a human leg.")]
		private float legEfficiency = 1f;
		public float LegEfficiency => legEfficiency;

		private PlayerHealthV2 playerHealth;

		public bool IsLeg = false;

		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			bodyPart = GetComponent<BodyPart>();
			playerHealth = bodyPart.HealthMaster as PlayerHealthV2;
			bodyPart.ModifierChange += ModifierChanged;
			playerHealth.OrNull()?.PlayerMove.AddModifier(this);
			if(IsLeg) playerHealth.OrNull()?.PlayerMove.AddLeg(this);
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			base.RemovedFromBody(livingHealth);
			if(IsLeg) playerHealth.OrNull()?.PlayerMove.RemoveLeg(this);
		}

		public void SetNewSpeeds(float newRunningSpeed, float newWalkingSpeed, float newCrawlingSpeed)
		{
			runningSpeed = newRunningSpeed;
			walkingSpeed = newWalkingSpeed;
			crawlingSpeed = newCrawlingSpeed;
			ModifierChanged();
		}

		public float RunningSpeedModifier => runningSpeed * legEfficiency * bodyPart.TotalModified;

		public float WalkingSpeedModifier => walkingSpeed * legEfficiency * bodyPart.TotalModified;

		public float CrawlingSpeedModifier => crawlingSpeed * armEfficiency * bodyPart.TotalModified;

		public void ModifierChanged()
		{
			playerHealth.PlayerMove.UpdateSpeeds();
		}
	}

}
