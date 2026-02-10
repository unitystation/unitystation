using UnityEngine;
using US13.Clothing;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Health.Objects;
using US13.Items.Traits;
using US13.UI.Core.ProgressBar;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.Items.Others.Magical.SpellBooks.potions
{
	public class PotionChillPotion : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		public ItemTrait AppliedItemTrait;

		private int Uses = 3;

		public Color PotionColour = new Color(0.23529411764f, 0.53725f, 0.815686274f);

		private static readonly StandardProgressActionConfig ProgressConfig =
			new StandardProgressActionConfig(StandardProgressActionType.SelfHeal);

		public virtual bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return interaction.Intent == Intent.Help;
		}

		public virtual void ServerPerformInteraction(HandApply interaction)
		{
			if (CheckTarget(interaction.TargetObject))
			{
				void ProgressComplete()
				{
					ServerApplyPotion(interaction.TargetObject);
				}

				StandardProgressAction.Create(ProgressConfig, ProgressComplete)
					.ServerStartProgress(interaction.Performer.RegisterTile(), 5f,
						interaction.Performer); //TODO Think about
			}

		}


		public void ServerApplyPotion(GameObject Target)
		{
			if (CheckTarget(Target))
			{
				var Integrity = Target.GetComponent<Integrity>();

				Integrity.Armor.Fire = 100;
				Integrity.Armor.TemperatureProtectionInK.y = 50000.0f;

				var WearableArmor = Target.GetComponent<WearableArmor>();

				if (WearableArmor != null)
				{
					foreach (var ABP in WearableArmor.ArmoredBodyParts)
					{
						ABP.Armor.Fire = 100;
						ABP.Armor.TemperatureProtectionInK.y = 50000.0f;
					}
				}

				var Sprites = Target.GetComponentsInChildren<SpriteHandler>();

				foreach (var Sprite in Sprites)
				{
					Sprite.SetColor(PotionColour);
				}

				var ItemAttributesV2 = Target.GetComponent<ItemAttributesV2>();


				if (ItemAttributesV2 != null)
				{
					ItemAttributesV2.AddTrait(AppliedItemTrait);
				}

				Uses--;


				if (Uses <= 0)
				{
					_ = Despawn.ServerSingle(this.gameObject);
				}
			}
		}


		public bool CheckTarget(GameObject Target)
		{
			if (Validations.HasComponent<Integrity>(Target) == false) return false;
			if (Validations.HasItemTrait(Target, AppliedItemTrait) == false) return false;
			return true;

		}
	}
}