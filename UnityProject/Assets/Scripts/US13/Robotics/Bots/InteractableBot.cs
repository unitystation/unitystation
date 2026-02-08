using Logs;
using Mirror;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Items.Cards;
using US13.Items.Traits;
using US13.NPC.AI;
using US13.UI.Systems.MainHUD.UI_Bottom;

namespace US13.Robotics.Bots
{
	public class InteractableBot : NetworkBehaviour, ICheckedInteractable<HandApply>
	{

		[SerializeField] private SpriteHandler spriteHandler;

		private MobExplore mobController;
		public MobExplore MobController
		{
			get
			{
				if (!mobController)
				{
					mobController = GetComponent<MobExplore>();
				}
				return mobController;
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;

			return MobController != null;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag)
				&& interaction.HandObject.TryGetComponent<Emag>(out var emag)
				&& emag.EmagHasCharges())
			{
				PerformEmag(emag, interaction);
			}
		}

		public void PerformEmag(Emag emag, HandApply interaction)
		{
			if (MobController == null) return;

			MobController.IsEmagged = true;
			emag.UseCharge(interaction);
			Chat.AddActionMsgToChat(interaction,
					"The bot's behavior controls disengage. The bot begins to rattle and smolder",
							"You can smell caustic smoke from somewhere...");

			if (spriteHandler == null)
			{
				Loggy.Warning($"{nameof(SpriteHandler)} missing on {gameObject}!", Category.Mobs);
				return;
			}

			spriteHandler.SetCatalogueIndexSprite(1, true);

		}
	}
}
