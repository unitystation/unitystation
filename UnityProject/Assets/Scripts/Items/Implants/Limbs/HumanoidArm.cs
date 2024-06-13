using System.Collections.Generic;
using Player.Movement;
using UnityEngine;

namespace HealthV2.Limbs
{
	public class HumanoidArm : Limb, IMovementEffect
	{
		[SerializeField]
		[Tooltip("A generalized number representing how efficient an arm this limb is.\n" +
		         "1 is a human hand.")]
		private float armEfficiency = 1f;
		public float ArmEfficiency => armEfficiency;

		public float RunningSpeedModifier { get; }
		public float WalkingSpeedModifier { get; }
		public float CrawlingSpeedModifier => crawlingSpeed * armEfficiency * RelatedPart.TotalModified;

		[SerializeField] [Tooltip("The crawling speed used for when the limb is attached as an arm.\n")]
		private float crawlingSpeed = 0.3f;
		public float CrawlingSpeed => crawlingSpeed;

		[Header("Arm Damage Stats")]
		[SerializeField] public float ArmMeleeDamage = 5f;
		[SerializeField] public DamageType ArmDamageType = DamageType.Brute;
		[SerializeField] public List<string> ArmDamageVerbs;
		[SerializeField] public TraumaticDamageTypes ArmTraumaticDamage;
		[SerializeField] public float ArmTraumaticChance = 0;


		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			base.OnAddedToBody(livingHealth);
			playerHealth.OrNull()?.PlayerMove.AddModifier(this);
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			base.OnRemovedFromBody(livingHealth);
			(livingHealth as PlayerHealthV2).OrNull()?.PlayerMove.RemoveModifier(this);
		}

		public void SetNewSpeeds(float newCrawlingSpeed)
		{
			crawlingSpeed = newCrawlingSpeed;
			ModifierChanged();
		}

		public void SetNewEfficiency(float newArmEfficiency)
		{
			armEfficiency = newArmEfficiency;
			ModifierChanged();
		}
	}
}