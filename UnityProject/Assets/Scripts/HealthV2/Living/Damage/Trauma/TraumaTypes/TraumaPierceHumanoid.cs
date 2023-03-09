﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2.TraumaTypes
{
	/// <summary>
	/// PIERCE LOOKS IDENTICAL TO SLASH BECAUSE INFECTIONS IS NOT PRESENT
	/// AND DISEMBOWELMENT HAVE NOT BEEN ADDED IN BACK YET.
	/// </summary>
	public class TraumaPierceHumanoid : TraumaLogic
	{
		[SerializeField] private float minimumDamageToProgressStages = 18f;
		[SerializeField] private float healingTimeFirstStage = 20f;
		[SerializeField] private float healingTimeSecondStage = 60f;

		private bool alreadyHealed = false;

		public override void OnTakeDamage(BodyPartDamageData data)
		{
			base.OnTakeDamage(data);
			if ( DMMath.Prob(data.TraumaDamageChance) == false ) return;
			if ( bodyPart.HealthMaster == null ) return;
			if ( data.DamageAmount < minimumDamageToProgressStages ) return;
			if ( data.AttackType is not (AttackType.Bullet or AttackType.Melee) ) return;
			if ( data.DamageType is not (DamageType.Brute) ) return;
			if ( CheckArmourStatus() ) return;
			GenericStageProgression();
		}

		private bool CheckArmourStatus()
		{
			var percent = CalculateDismembermentProtection(bodyPart.ClothingArmors);
			if ( percent.IsBetween(0, 0.95f) ) return false;
			return DMMath.Prob(percent);
		}

		private static float CalculateDismembermentProtection(LinkedList<Armor> armors)
		{
			var protection = 0f;
			foreach (var armor in armors)
			{
				protection += armor.DismembermentProtectionChance;
			}

			return Mathf.Clamp(protection, 0, 100);
		}

		public override void ProgressDeadlyEffect()
		{
			if (currentStage == 3) return;
			base.ProgressDeadlyEffect();
			currentStage++;
			switch (currentStage)
			{
				case 0:
					break;
				case 1:
					bodyPart.HealthMaster.ChangeBleedStacks(3);
					Chat.AddActionMsgToChat(bodyPart.HealthMaster.gameObject,
						$"<color=red>{bodyPart.HealthMaster.playerScript.visibleName}'s " +
						$"{bodyPart.gameObject.ExpensiveName()} starts dripping blood lightly as a small hole opens up.</color>");
					StartCoroutine(NaturalHealing(healingTimeFirstStage));
					break;
				case 2:
					bodyPart.HealthMaster.ChangeBleedStacks(8);
					Chat.AddActionMsgToChat(bodyPart.HealthMaster.gameObject,
						$"<color=red>Blood heavily pours out of {bodyPart.HealthMaster.playerScript.visibleName}'s " +
						$"{bodyPart.gameObject.ExpensiveName()} as flesh opens up, revealing meat and fat.</color>");
					StopCoroutine(nameof(NaturalHealing));
					StartCoroutine(NaturalHealing(healingTimeSecondStage));
					break;
				case 3:
					bodyPart.HealthMaster.ChangeBleedStacks(18);
					Chat.AddActionMsgToChat(bodyPart.HealthMaster.gameObject,
						$"<size=+6><color=red>A giant hole opens up as {bodyPart.HealthMaster.playerScript.visibleName}'s " +
						$"{bodyPart.gameObject.ExpensiveName()} has all of its insides exposed!</color></size>");
					StopCoroutine(nameof(NaturalHealing));
					break;
			}

			foreach (var organ in bodyPart.OrganList)
			{
				organ.RelatedPart.TakeDamage(null, Random.Range(3,6),
					AttackType.Internal, DamageType.Brute, true, true, 100);
			}
		}

		private IEnumerator NaturalHealing(float healingTime)
		{
			yield return WaitFor.Seconds(healingTime);
			if ( alreadyHealed ) yield break;
			HealStage();
			if ( bodyPart.HealthMaster == null ) yield break;
			Chat.AddExamineMsg(bodyPart.HealthMaster.gameObject, "You notice your wound slightly close up on its own.");
		}

		public override string StageDescriptor()
		{
			return currentStage switch
			{
				0 => null,
				1 => $"{bodyPart.gameObject.ExpensiveName()} - Minor Breakage.",
				2 => $"{bodyPart.gameObject.ExpensiveName()} - Open Puncture.",
				3 => $"{bodyPart.gameObject.ExpensiveName()} - Ruptured Cavity.",
				_ => null
			};
		}
	}
}
