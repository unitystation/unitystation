using System.Collections.Generic;
using UnityEngine;

namespace HealthV2.TraumaTypes
{
	public class TraumaBurning : TraumaLogic
	{

		[SerializeField] private Color bodyPartColorWhenCharred;
		[SerializeField] private Color bodyPartColorWhenSecondDegreeBurns;
		[SerializeField] private Color bodyPartColorWhenThirdBurns;

		public override void OnTakeDamage(float damage, DamageType damageType, AttackType attackType)
		{
			base.OnTakeDamage(damage, damageType, attackType);
			if (CheckArmourChance() == false) return;
			GenericStageProgression();
		}

		private bool CheckArmourChance()
		{
			var percent = 0f;
			foreach (var armor in bodyPart.ClothingArmors)
			{
				percent += armor.Fire;
				percent += armor.Energy;
			}

			percent += bodyPart.SelfArmor.Fire;
			percent += bodyPart.SelfArmor.Energy;
			return DMMath.Prob(percent);
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
					bodyPart.ChangeBodyPartColor(bodyPartColorWhenSecondDegreeBurns);
					bodyPart.BodyPartItemSprite.OrNull()?.SetColor(bodyPartColorWhenSecondDegreeBurns);
					Chat.AddActionMsgToChat(bodyPart.HealthMaster.playerScript.gameObject,
						"<color=red>You feel your limb tingle as its color changes.</color>",
						$"{bodyPart.HealthMaster.playerScript.visibleName}'s limb turns a different color.");
					break;
				case 2:
					bodyPart.ChangeBodyPartColor(bodyPartColorWhenThirdBurns);
					bodyPart.BodyPartItemSprite.OrNull()?.SetColor(bodyPartColorWhenThirdBurns);
					Chat.AddActionMsgToChat(bodyPart.HealthMaster.playerScript.gameObject,
						"<color=red>You feel as if your limb is boiling from the inside as a sharp pain overtakes it like a knife stab.</color>",
						$"{bodyPart.HealthMaster.playerScript.visibleName}'s limb turns a darker color.");
					break;
				case 3:
					bodyPart.ChangeBodyPartColor(bodyPartColorWhenCharred);
					bodyPart.BodyPartItemSprite.OrNull()?.SetColor(bodyPartColorWhenCharred);
					Chat.AddActionMsgToChat(bodyPart.HealthMaster.playerScript.gameObject,
						"<size=+6><color=red>You feel your limb grow weaker as all senses start slowly disappearing from it.</color></size>",
						$"{bodyPart.HealthMaster.playerScript.visibleName}'s limb turns chars from excessive burn damage..");
					break;
				case 4:
					if (DMMath.Prob(75))
					{
						currentStage = 3;
						Chat.AddActionMsgToChat(bodyPart.HealthMaster.gameObject,
							$"<color=red>Parts of the {bodyPart.gameObject.ExpensiveName()} crumble and ash away.</color");
						return;
					}
					AshBodyPart();
					break;
			}
		}

		/// <summary>
		/// Turns this body part into ash while protecting items inside of that cannot be ashed.
		/// </summary>
		private void AshBodyPart()
		{

			if (bodyPart.BodyPartType == BodyPartType.Chest || bodyPart.BodyPartType == BodyPartType.Head) return; //TODO is temporary weighting on Trauma discussion
			var internalItemList = bodyPart.OrganStorage.GetItemSlots();
			foreach (var item in internalItemList)
			{
				var itemObject = item.ItemObject.OrNull()?.GetComponent<Integrity>();
				if (itemObject == null) continue; //Incase this is an empty slot
				if (itemObject.Resistances.FireProof || itemObject.Resistances.Indestructable)
				{
					Inventory.ServerDrop(item);
				}
			}

			Chat.AddActionMsgToChat(bodyPart.HealthMaster.gameObject,
				$"{bodyPart.HealthMaster.playerScript.visibleName}'s {bodyPart.gameObject.ExpensiveName()} ashes away.");

			_ = Spawn.ServerPrefab(bodyPart.OrganStorage.AshPrefab, bodyPart.HealthMaster.RegisterTile.WorldPosition);
			bodyPart.HealthMaster.DismemberBodyPart(bodyPart);
			_ = Despawn.ServerSingle(bodyPart.gameObject);
		}
	}
}