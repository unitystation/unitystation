using System;
using Mirror;
using NaughtyAttributes;
using Systems.Explosions;
using UnityEngine;

namespace Items.Storage
{
	public class Lockbox : NetworkBehaviour, IInteractable<HandActivate>,
		ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>
	{
		[SyncVar] private bool isLocked = true;
		[SyncVar] private bool isEmagged = false;

		private InteractableStorage interactableStorage;
		private SpriteHandler spriteHandler;

		[SerializeField] private Access allowedAccess;
		[SerializeField] private SpriteDataSO lockedSprite;
		[SerializeField] private SpriteDataSO unlockedSprite;

		private void Awake()
		{
			interactableStorage = GetComponent<InteractableStorage>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (isLocked == false)
			{
				interactableStorage.Interact(interaction);
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
			if(interaction.UsedObject.TryGetComponent<IDCard>(out var card) == false ||
			   interaction.UsedObject.TryGetComponent<Emag>(out var mag)) return;
			if (card != null && card.HasAccess(allowedAccess) == false)
			{
				Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} peeps as it refuses access from this card.");
				return;
			}

			if (mag != null && mag.UseCharge(interaction))
			{
				isLocked = false;
				isEmagged = true;
				spriteHandler.SetSpriteSO(unlockedSprite);
				SparkUtil.TrySpark(interaction.Performer);
				return;
			}
			isLocked = !isLocked;
			spriteHandler.SetSpriteSO(isLocked ? lockedSprite : unlockedSprite);
			Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} beeps as it accepts this card.");
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (interaction.UsedObject != null)
			{
				if (interaction.UsedObject.TryGetComponent<IDCard>(out var card) && card.HasAccess(allowedAccess) == false)
				{
					Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} beeps as it refuses access from this card.");
					return;
				}
				if(card != null && card.HasAccess(allowedAccess))
				{
					isLocked = !isLocked;
					spriteHandler.SetSpriteSO(isLocked ? lockedSprite : unlockedSprite);
					Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} beeps as it accepts this card.");
					return;
				}
				if (interaction.UsedObject.TryGetComponent<Emag>(out var mag) && mag.UseCharge(gameObject, interaction.Performer))
				{
					isLocked = false;
					isEmagged = true;
					spriteHandler.SetSpriteSO(unlockedSprite);
					SparkUtil.TrySpark(interaction.Performer);
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