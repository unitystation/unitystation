using System.Collections;
using UnityEngine;
using Mirror;
using Systems.MobAIs;
using Items;
using Logs;

namespace Robotics
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
				Loggy.LogWarning($"{nameof(SpriteHandler)} missing on {gameObject}!", Category.Mobs);
				return;
			}

			spriteHandler.ChangeSprite(1, true);

		}
	}
}
