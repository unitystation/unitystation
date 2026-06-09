using UnityEngine;
using US13.Clothing;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.UI.Core.ProgressBar;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.Items.Others.Magical.SpellBooks.potions
{
	public class PotionSpeedPotion : MonoBehaviour, ICheckedInteractable<HandApply>
	{

		public Color PotionColour = new Color(1f, 0.992156f, 0.0039f);

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
				var WearableSpeedDebuff = Target.GetComponent<WearableSpeedDebuff>();

				WearableSpeedDebuff.SpeedDebuffRemoved = true;

				var Sprites = Target.GetComponentsInChildren<SpriteHandler>();

				foreach (var Sprite in Sprites)
				{
					Sprite.SetColor(PotionColour);
				}

				_ = Despawn.ServerSingle(this.gameObject);
			}
		}


		public bool CheckTarget(GameObject Target)
		{
			var WearableSpeedDebuff = Target.GetComponent<WearableSpeedDebuff>();
			if (WearableSpeedDebuff == null) return false;
			if (WearableSpeedDebuff.SpeedDebuffRemoved) return false;
			return true;

		}
	}
}
