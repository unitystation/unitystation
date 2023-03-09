using UnityEngine;
using HealthV2;
using AddressableReferences;

namespace Items.Implants.Organs
{
	public class ReviverImplant : BodyPartFunctionality
	{

		[SerializeField] private UniversalObjectPhysics objectPhysics;

		[SerializeField] private AddressableAudioSource reviveSound;

		[SerializeField] private int delaySeconds = 600;

		private float lastTrigger = 0;

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			lastTrigger = Time.time - delaySeconds; //Ready to revive
		}

		public override void ImplantPeriodicUpdate()
		{
			if ((RelatedPart.HealthMaster.IsCrit == false && RelatedPart.HealthMaster.IsSoftCrit == false) || (Time.time - lastTrigger) <= 600) return;
			lastTrigger = Time.time;

			RelatedPart.HealthMaster.HealDamageOnAll(gameObject, 25, DamageType.Tox);
			RelatedPart.HealthMaster.HealDamageOnAll(gameObject, 25, DamageType.Brute);
			RelatedPart.HealthMaster.HealDamageOnAll(gameObject, 25, DamageType.Burn);

			_ = SoundManager.PlayNetworkedAtPosAsync(reviveSound, objectPhysics.OfficialPosition);
		}

		public override void EmpResult(int strength)
		{
			//Heart heart = RelatedPart.HealthMaster.reagentPoolSystem?.PumpingDevices?.PickRandom();
			Heart heart = RelatedPart.HealthMaster.CirculatorySystem.Hearts.PickRandom();

			heart.OrNull()?.DoHeartAttack();
		}
	}
}
