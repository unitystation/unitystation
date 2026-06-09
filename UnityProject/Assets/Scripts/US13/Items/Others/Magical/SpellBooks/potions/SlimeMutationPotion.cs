using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs;
using US13.UI.Core.ProgressBar;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.Items.Others.Magical.SpellBooks.potions
{
	public class SlimeMutationPotion : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private static readonly StandardProgressActionConfig ProgressConfig =
			new StandardProgressActionConfig(StandardProgressActionType.SelfHeal);

		public virtual bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject) ==false) return false;
			if (interaction.TargetObject.GetComponent<LivingHealthMasterBase>().IsDead) return false;
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
					.ServerStartProgress(interaction.Performer.RegisterTile(), 5f, interaction.Performer); //TODO Think about
			}

		}


		public void ServerApplyPotion(GameObject Target)
		{
			if (CheckTarget(Target))
			{
				var core = Target.GetComponent<LivingHealthMasterBase>().brain.GetComponent<SlimeCore>();
				core.DeStabilised = true;
				_ = Despawn.ServerSingle(this.gameObject);
			}
		}


		public bool CheckTarget(GameObject Target)
		{
			if (Validations.HasComponent<LivingHealthMasterBase>(Target) ==false) return false;
			if (Target.GetComponent<LivingHealthMasterBase>().IsDead) return false;
			var core = Target.GetComponent<LivingHealthMasterBase>().brain.GetComponent<SlimeCore>();
			if (core == null) return false;
			if (core.Stabilised) return false;
			if (core.DeStabilised) return false;
			return true;

		}

	}
}
