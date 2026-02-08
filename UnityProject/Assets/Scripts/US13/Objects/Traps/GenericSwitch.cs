using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using Util;

namespace US13.Objects.Traps
{
	public class GenericSwitch : GenericTriggerOutput, ICheckedInteractable<HandApply>
	{
		private bool state = false;
		[SerializeField] private SpriteHandler spriteHandler = null;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side, AllowTelekinesis: false) == false) return false;

			// only allow interactions targeting this
			if (interaction.TargetObject != gameObject) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			ToggleSwitch();
			Chat.AddExamineMsgFromServer(interaction.Performer, $"You flick the {gameObject.ExpensiveName()}");
		}

		public void ToggleSwitch()
		{
			state = !state;
			if (state == true)
			{
				TriggerOutput();
				spriteHandler.SetSpriteVariant(1);
				return;
			}

			ReleaseOutput();
			spriteHandler.SetSpriteVariant(0);
		}
	}
}
