using System;
using HealthV2;
using Player.Movement;
using UnityEngine;

namespace HealthV2
{
	public class Limb : BodyPartFunctionality
	{
		protected PlayerHealthV2 playerHealth;

		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			RelatedPart = GetComponent<BodyPart>();
			playerHealth = RelatedPart.HealthMaster as PlayerHealthV2;
			RelatedPart.ModifierChange += ModifierChanged;
		}

		public void ModifierChanged()
		{
			playerHealth.PlayerMove.UpdateSpeeds();
		}
	}
}
