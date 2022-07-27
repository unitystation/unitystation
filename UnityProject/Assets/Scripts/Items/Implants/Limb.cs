using System;
using HealthV2;
using Player.Movement;
using UnityEngine;

namespace HealthV2
{
	public class Limb : BodyPartFunctionality, IMovementEffect
	{
		protected PlayerHealthV2 playerHealth;

		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			bodyPart = GetComponent<BodyPart>();
			playerHealth = bodyPart.HealthMaster as PlayerHealthV2;
			bodyPart.ModifierChange += ModifierChanged;
			playerHealth.OrNull()?.PlayerMove.AddModifier(this);
		}

		public void ModifierChanged()
		{
			playerHealth.PlayerMove.UpdateSpeeds();
		}

		public float RunningSpeedModifier { get; }
		public float WalkingSpeedModifier { get; }
		public float CrawlingSpeedModifier { get; }
	}

}
