using System.Collections.Generic;
using UnityEngine;

namespace HealthV2.TraumaTypes
{
	public class TraumaSlashHumanoid : TraumaLogic
	{

		[SerializeField] private float minimumDamageToProgressStages = 20f;

		public override void OnTakeDamage(float damage, DamageType damageType, AttackType attackType)
		{
			base.OnTakeDamage(damage, damageType, attackType);
			if (damage < minimumDamageToProgressStages) return;
			if (attackType is not (AttackType.Bullet or AttackType.Melee)) return;
			if (damageType is not (DamageType.Brute or DamageType.Burn)) return;
			if (CheckArmourStatus() == false) return;
			GenericStageProgression();
		}

		private bool CheckArmourStatus()
		{
			var percent = CalculateDismembermentProtection(bodyPart.ClothingArmors);
			return percent.IsBetween(0, 0.55f) || DMMath.Prob(percent);
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
					Chat.AddActionMsgToChat(bodyPart.HealthMaster.playerScript.gameObject,
						$"<color=red>{bodyPart.HealthMaster.playerScript.visibleName}'s {bodyPart.gameObject.ExpensiveName()} starts dripping blood lightly as it gets slightly cut open.</color>");
					break;
				case 2:
					bodyPart.HealthMaster.ChangeBleedStacks(8);
					Chat.AddActionMsgToChat(bodyPart.HealthMaster.playerScript.gameObject,
						$"<color=red>Blood heavily pours out of {bodyPart.HealthMaster.playerScript.visibleName}'s {bodyPart.gameObject.ExpensiveName()} as a huge gash can be seen open.</color>");
					break;
				case 3:
					bodyPart.HealthMaster.ChangeBleedStacks(35);
					Chat.AddActionMsgToChat(bodyPart.HealthMaster.playerScript.gameObject,
						$"<size=+6><color=red>{bodyPart.HealthMaster.playerScript.visibleName}'s {bodyPart.gameObject.ExpensiveName()} becomes heavily mangled and torn!</color></size>");
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

		private static float CalculateDismembermentProtection(LinkedList<Armor> armors)
		{
			var protection = 0f;
			foreach (var armor in armors)
			{
				protection += armor.DismembermentProtectionChance;
			}

			return Mathf.Clamp(protection, 0, 100);
		}
	}
}