using Logs;
using UnityEngine;

namespace HealthV2.TraumaTypes
{
	public class TraumaRadiationAfflict : TraumaLogic
	{
		[SerializeField,Tooltip("The radiation damage dealt is reduced by this factor to calculate added pathogen count.")]
		private float afflictionReductionFactor = 400f;

		private const float DeadlyCancerCount = 25f;

		[SerializeField]
		private float MinThresholdDamage = 2;
		[SerializeField]
		private float CancerPercentage = 0.5f;
		public override void OnTakeDamage(BodyPartDamageData data)
		{
			if (data.DamageAmount < 2) return;
			if ( data.TramuticDamageType != TraumaticDamageTypes.NONE ) return;
			if ( data.AttackType != AttackType.Rad ) return;
			if ( DMMath.Prob(GetRadProtectionPercentage()) ) return;
			if ( DMMath.Prob(data.TraumaDamageChance) == false ) return;
			if ( DMMath.Prob(CancerPercentage) == false ) return;
			Loggy.Error(data.DamageAmount.ToString());
			if ( deadlyDamageInOneHit > data.DamageAmount)
			{
				AfflictMinorCancer(data);
				return;
			}
			ProgressDeadlyEffect();
		}

		private void AfflictMinorCancer(BodyPartDamageData data)
		{
			bodyPart.HealthMaster.reagentPoolSystem.BloodPool.Add(CommonSicknesses.Instance.SpaceCancerReagent, data.DamageAmount / afflictionReductionFactor);
		}

		public override void ProgressDeadlyEffect()
		{
			bodyPart.HealthMaster.reagentPoolSystem.BloodPool.Add(CommonSicknesses.Instance.SpaceCancerReagent, DeadlyCancerCount);
		}

		private float GetRadProtectionPercentage()
		{
			var percent = 0f;
			foreach (var armor in bodyPart.ClothingArmors)
			{
				percent += armor.Rad;
			}
			return percent;
		}
	}
}