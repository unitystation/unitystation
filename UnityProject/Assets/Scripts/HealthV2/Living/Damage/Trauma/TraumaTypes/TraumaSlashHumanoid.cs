using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2.TraumaTypes
{
	public class TraumaSlashHumanoid : TraumaLogic
	{

		[SerializeField] private float minimumDamageToProgressStages = 18f;
		[SerializeField] private float healingTimeFirstStage = 20f;
		[SerializeField] private float healingTimeSecondStage = 60f;

		private bool alreadyHealed = false;

		public override void OnTakeDamage(float damage, DamageType damageType, AttackType attackType)
		{
			base.OnTakeDamage(damage, damageType, attackType);
			if ( bodyPart.HealthMaster == null ) return;
			if ( damage < minimumDamageToProgressStages ) return;
			if ( attackType is not (AttackType.Bullet or AttackType.Melee) ) return;
			if ( damageType is not (DamageType.Brute or DamageType.Burn) ) return;
			if ( CheckArmourStatus() ) return;
			GenericStageProgression();
		}

		private bool CheckArmourStatus()
		{
			var percent = CalculateDismembermentProtection(bodyPart.ClothingArmors);
			if ( percent.IsBetween(0, 0.95f) ) return true;
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
						$"{bodyPart.gameObject.ExpensiveName()} starts dripping blood lightly as it gets slightly cut open.</color>");
					StartCoroutine(NaturalHealing(healingTimeFirstStage));
					break;
				case 2:
					bodyPart.HealthMaster.ChangeBleedStacks(9);
					Chat.AddActionMsgToChat(bodyPart.HealthMaster.gameObject,
						$"<color=red>Blood heavily pours out of {bodyPart.HealthMaster.playerScript.visibleName}'s " +
						$"{bodyPart.gameObject.ExpensiveName()} as a huge gash can be seen open.</color>");
					StopCoroutine(nameof(NaturalHealing));
					StartCoroutine(NaturalHealing(healingTimeSecondStage));
					break;
				case 3:
					bodyPart.HealthMaster.ChangeBleedStacks(27);
					Chat.AddActionMsgToChat(bodyPart.HealthMaster.gameObject,
						$"<size=+6><color=red>{bodyPart.HealthMaster.playerScript.visibleName}'s " +
						$"{bodyPart.gameObject.ExpensiveName()} becomes heavily mangled and torn!</color></size>");
					StopCoroutine(nameof(NaturalHealing));
					break;
				case 4:
					if (DMMath.Prob(75))
					{
						currentStage = 3;
						return;
					}
					bodyPart.TryRemoveFromBody();
					break;
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
				1 => $"{bodyPart.gameObject.ExpensiveName()} - Rough Abrasion.",
				2 => $"{bodyPart.gameObject.ExpensiveName()} - Open Laceration.",
				3 => $"{bodyPart.gameObject.ExpensiveName()} - Weeping Avulsion.",
				_ => null
			};
		}
	}
}