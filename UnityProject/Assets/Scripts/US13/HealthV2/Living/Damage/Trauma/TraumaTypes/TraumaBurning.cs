using UnityEngine;
using US13.Core.Chat;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Health.Objects;
using US13.Systems.Inventory;
using Util;
using Util.Independent.FluentRichText;

namespace US13.HealthV2.Living.Damage.Trauma.TraumaTypes
{
	public class TraumaBurning : TraumaLogic
	{

		[SerializeField] private Color bodyPartColorWhenCharred;
		[SerializeField] private Color bodyPartColorWhenSecondDegreeBurns;
		[SerializeField] private Color bodyPartColorWhenThirdBurns;

		public override void OnTakeDamage(BodyPartDamageData data)
		{
			base.OnTakeDamage(data);
			if ( DMMath.Prob(data.TraumaDamageChance) == false ) return;
			if ( CheckArmourChance() ) return;
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
			if ( percent.IsBetween(0, 0.95f) ) return false;
			return DMMath.Prob(percent);
		}

		public override void ProgressDeadlyEffect()
		{
			if (currentStage == 4) return;
			base.ProgressDeadlyEffect();
			currentStage++;
			switch (currentStage)
			{
				case 0:
					break;
				case 1:
					bodyPart.ChangeBodyPartColor(bodyPartColorWhenSecondDegreeBurns);
					GameObjectExtensions.OrNull<SpriteHandler>(bodyPart.BodyPartItemSprite)?.SetColor(bodyPartColorWhenSecondDegreeBurns);
					Chat.AddActionMsgToChat((GameObject)bodyPart.HealthMaster.playerScript.gameObject,
						"<color=red>You feel your limb tingle as its color changes.</color>",
						$"{bodyPart.HealthMaster.playerScript.visibleName}'s limb turns a different color.");
					break;
				case 2:
					bodyPart.ChangeBodyPartColor(bodyPartColorWhenThirdBurns);
					GameObjectExtensions.OrNull<SpriteHandler>(bodyPart.BodyPartItemSprite)?.SetColor(bodyPartColorWhenThirdBurns);
					Chat.AddActionMsgToChat((GameObject)bodyPart.HealthMaster.playerScript.gameObject,
						"<color=red>You feel as if your limb is boiling from the inside as a sharp pain overtakes it like a knife stab.</color>",
						$"{bodyPart.HealthMaster.playerScript.visibleName}'s limb turns a darker color.");
					break;
				case 3:
					bodyPart.ChangeBodyPartColor(bodyPartColorWhenCharred);
					GameObjectExtensions.OrNull<SpriteHandler>(bodyPart.BodyPartItemSprite)?.SetColor(bodyPartColorWhenCharred);
					Chat.AddActionMsgToChat((GameObject)bodyPart.HealthMaster.playerScript.gameObject,
						"You feel your limb grow weaker as all senses start slowly disappearing from it.".Color(Color.red).FontSize("+6"),
						$"{bodyPart.HealthMaster.playerScript.visibleName}'s limb turns chars from excessive burn damage..");
					break;
				case 4:
					if (DMMath.Prob(75))
					{
						currentStage = 3;
						Chat.AddActionMsgToChat(bodyPart.HealthMaster.gameObject,
							$"Parts of the {SweetExtensions.ExpensiveName(bodyPart.gameObject)} crumble and ash away.".Color(Color.red));
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
			if (bodyPart.BodyPartType == BodyPartType.Head) return;
			if (bodyPart.BodyPartType == BodyPartType.Chest) return;
			var internalItemList = bodyPart.OrganStorage.GetItemSlots();
			foreach (var item in internalItemList)
			{
				var itemObject = GameObjectExtensions.OrNull<GameObject>(item.ItemObject)?.GetComponent<Integrity>();
				if (itemObject == null) continue; //Incase this is an empty slot
				if (itemObject.Resistances.FireProof || itemObject.Resistances.Indestructable)
				{
					Inventory.ServerDrop(item);
				}
			}

			Chat.AddActionMsgToChat(bodyPart.HealthMaster.gameObject,
				$"{bodyPart.HealthMaster.playerScript.visibleName}'s {SweetExtensions.ExpensiveName(bodyPart.gameObject)} ashes away.");

			_ = Spawn.ServerPrefab(bodyPart.OrganStorage.AshPrefab, bodyPart.HealthMaster.RegisterTile.WorldPosition);
			bodyPart.HealthMaster.DismemberBodyPart(bodyPart);
			_ = Despawn.ServerSingle(bodyPart.gameObject);
		}

		public override string StageDescriptor()
		{
			return currentStage switch
			{
				0 => null,
				1 => $"{SweetExtensions.ExpensiveName(bodyPart.gameObject)} - Second Degree Burns.",
				2 => $"{SweetExtensions.ExpensiveName(bodyPart.gameObject)} - Third Degree Burns.",
				3 => $"{SweetExtensions.ExpensiveName(bodyPart.gameObject)} - Fourth Degree Burns. (Catastrophic Burns)",
				_ => null
			};
		}
	}
}