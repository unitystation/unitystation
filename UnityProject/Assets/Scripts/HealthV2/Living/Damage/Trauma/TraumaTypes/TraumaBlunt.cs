using System;
using Logs;
using UnityEngine;

namespace HealthV2.TraumaTypes
{
	public class TraumaBlunt : TraumaLogic
	{
		[SerializeField] private float minimumDamage = 14f;
		[SerializeField] private SerializableDictionary<int, BluntTraumaDamageInfo> internalDamagePerStage
			= new SerializableDictionary<int, BluntTraumaDamageInfo>();

		private void Start()
		{
			if (stages.Count != internalDamagePerStage.Count)
			{
				Loggy.LogWarning("[Health/Trauma/TraumaBlunt] - Mismatched number of stages and damage info. " +
				                  "NREs have a high chance of happening.", Category.Health);
			}
		}


		public override void OnTakeDamage(BodyPartDamageData data)
		{
			base.OnTakeDamage(data);
			if ( DMMath.Prob(data.TraumaDamageChance) == false ) return;
			if ( data.DamageAmount < minimumDamage ) return;
			if ( data.AttackType is not AttackType.Melee ) return;
			if ( data.DamageType is not DamageType.Brute ) return;
			if ( CheckArmourChance() ) return;

			GenericStageProgression();
		}

		private bool CheckArmourChance()
		{
			var percent = 0f;
			foreach (var armor in bodyPart.ClothingArmors)
			{
				percent += armor.Melee;
			}

			percent += bodyPart.SelfArmor.Melee;
			return DMMath.Prob(percent);
		}

		public override void ProgressDeadlyEffect()
		{
			base.ProgressDeadlyEffect();
			currentStage++;
			currentStage = Mathf.Clamp(currentStage, 0, stages.Count - 1);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, InternalOrganDamage);
			UpdateManager.Add(InternalOrganDamage, internalDamagePerStage[currentStage].TimeForDamage);
			Chat.AddExamineMsg(bodyPart.HealthMaster.gameObject, "<size=+6><color=red>You feel something inside you tears up</color></size>");
		}

		public override void HealStage()
		{
			base.HealStage();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, InternalOrganDamage);
			if ( currentStage == 0 ) return;
			UpdateManager.Add(InternalOrganDamage, internalDamagePerStage[currentStage].TimeForDamage);
		}

		private void InternalOrganDamage()
		{
			foreach (var organ in bodyPart.OrganList)
			{
				if ( currentStage <= 2 && DMMath.Prob(50) ) continue;
				organ.RelatedPart.TakeDamage(null, internalDamagePerStage[currentStage].Damage,
					AttackType.Internal, DamageType.Brute, false, false, 100, 0,
					TraumaticDamageTypes.NONE, false);
			}
		}

		public override string StageDescriptor()
		{
			return currentStage switch
			{
				0 => null,
				1 => $"{bodyPart.gameObject.ExpensiveName()} - Joint Dislocation.",
				2 => $"{bodyPart.gameObject.ExpensiveName()} - Hairline Fracture.",
				3 => $"{bodyPart.gameObject.ExpensiveName()} - Compound Fracture.",
				_ => null
			};
		}

		[Serializable]
		private struct BluntTraumaDamageInfo
		{
			public float Damage;
			public float TimeForDamage;
		}
	}
}