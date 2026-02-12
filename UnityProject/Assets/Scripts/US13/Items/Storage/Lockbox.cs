using Mirror;
using UnityEngine;
using US13.Clothing.BackPack;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Items.Cards;
using US13.Systems.Clearance;
using US13.Systems.Explosions;
using Util;

namespace US13.Items.Storage
{
	public class Lockbox : NetworkBehaviour, IInteractable<HandActivate>,
		ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>
	{
		[SerializeField]
		private SpriteDataSO lockedSprite;
		[SerializeField]
		private SpriteDataSO unlockedSprite;

		[SyncVar] private bool isLocked = true;
		[SyncVar] private bool isEmagged = false;
		private InteractableStorage interactableStorage;
		private SpriteHandler spriteHandler;
		private ClearanceRestricted restricted;

		private void Awake()
		{
			interactableStorage = GetComponent<InteractableStorage>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			restricted = GetComponent<ClearanceRestricted>();
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (isLocked == false)
			{
				interactableStorage.OpenInventoryInteraction(interaction);
				return;
			}
			Chat.AddExamineMsg(interaction.Performer, "This seems locked.");
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) != false && interaction.UsedObject != null && isEmagged == false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if(interaction.UsedObject.TryGetComponent<IClearanceSource>(out var card) == false ||
			   interaction.UsedObject.TryGetComponent<Emag>(out var mag)) return;

			if (mag != null && mag.UseCharge(interaction))
			{
				isLocked = false;
				isEmagged = true;
				spriteHandler.SetSpriteSO(unlockedSprite);
				SparkUtil.TrySpark(interaction.Performer);
				return;
			}

			restricted.PerformWithClearance(card,
				() =>
				{
					isLocked = !isLocked;
					spriteHandler.SetSpriteSO(isLocked ? lockedSprite : unlockedSprite);
					Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} beeps as it accepts this card.");
				},
				() =>
				{
					Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} beeps as it refuses access from this card.");
				});
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{

			if (interaction.UsedObject != null)
			{
				if (interaction.UsedObject.TryGetComponent<Emag>(out var mag) && mag.UseCharge(gameObject, interaction.Performer))
				{
					isLocked = false;
					isEmagged = true;
					spriteHandler.SetSpriteSO(unlockedSprite);
					SparkUtil.TrySpark(interaction.Performer);
					return;
				}

				if (interaction.UsedObject.TryGetComponent<IClearanceSource>(out var card) )
				{
					restricted.PerformWithClearance(card,
						() =>
						{
							isLocked = !isLocked;
							spriteHandler.SetSpriteSO(isLocked ? lockedSprite : unlockedSprite);
							Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} beeps as it accepts this card.");
						},
						() =>
						{
							Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} beeps as it refuses access from this card.");
						});

					return;
				}
			}

			Chat.AddExamineMsg(interaction.Performer, "This seems locked.");
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			return isLocked;
		}
	}
}