using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Lifecycle;
using US13.Items.Implants.Organs;
using US13.UI.Core.ProgressBar;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.Items.Others.Magical.SpellBooks.potions
{
	public class CoreEnhancer : MonoBehaviour
	{
		private static readonly StandardProgressActionConfig ProgressConfig =
			new StandardProgressActionConfig(StandardProgressActionType.SelfHeal);

		public virtual bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (Validations.HasComponent<SlimeCore>(interaction.TargetObject) == false) return false;
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

				var SlimeCore = Target.GetComponent<SlimeCore>();
				SlimeCore.Enhanced = true;
				SlimeCore.EnhancedUsedUp = false;

				_ = Despawn.ServerSingle(this.gameObject);

			}
		}


		public bool CheckTarget(GameObject Target)
		{
			var SlimeCore = Target.GetComponent<SlimeCore>();
			if (SlimeCore == null) return false;
			if (SlimeCore.Enhanced) return false;

			return true;

		}
	}
}
