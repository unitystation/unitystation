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

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			base.OnAddedToBody(livingHealth);
			playerHealth.OrNull()?.PlayerMove.AddModifier(this);


		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			base.RemovedFromBody(livingHealth);
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