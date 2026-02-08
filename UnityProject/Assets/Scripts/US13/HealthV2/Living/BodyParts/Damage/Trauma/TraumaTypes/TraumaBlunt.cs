using System;
using Logs;
using UnityEngine;
using US13.Core.Chat;
using US13.Health.Objects;
using US13.HealthV2.Living.BodyParts.Damage;
using US13.HealthV2.Living.BodyParts.Damage.Trauma;
using US13.Managers.UpdateManager;
using Util;

namespace US13.HealthV2.Living.Damage.Trauma.TraumaTypes
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
				Loggy.Warning("[Health/Trauma/TraumaBlunt] - Mismatched number of stages and damage info. " +
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
				1 => $"{SweetExtensions.ExpensiveName(bodyPart.gameObject)} - Joint Dislocation.",
				2 => $"{SweetExtensions.ExpensiveName(bodyPart.gameObject)} - Hairline Fracture.",
				3 => $"{SweetExtensions.ExpensiveName(bodyPart.gameObject)} - Compound Fracture.",
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