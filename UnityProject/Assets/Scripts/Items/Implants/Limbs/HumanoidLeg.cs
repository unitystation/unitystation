using Player.Movement;
using UnityEngine;

namespace HealthV2.Limbs
{
	public class HumanoidLeg : Limb
	{

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			base.OnAddedToBody(livingHealth);
			playerHealth.OrNull()?.PlayerMove.AddLeg(this);
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			base.OnRemovedFromBody(livingHealth);
			playerHealth.OrNull()?.PlayerMove.RemoveLeg(this);
		}

		public void SetNewSpeeds(float newRunningSpeed, float newWalkingSpeed)
		{
			runningSpeed = newRunningSpeed;
			walkingSpeed = newWalkingSpeed;
			ModifierChanged();
		}
	}
}