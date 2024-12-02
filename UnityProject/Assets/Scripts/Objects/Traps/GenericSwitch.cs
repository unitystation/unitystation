using UnityEngine;

namespace Objects.Traps
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
