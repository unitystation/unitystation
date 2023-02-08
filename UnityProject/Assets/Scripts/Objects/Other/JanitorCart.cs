using System;
using AddressableReferences;
using Chemistry.Components;
using UnityEngine;

namespace Objects.Other
{
	public class JanitorCart : ReagentContainer, ICheckedInteractable<HandApply>
	{
		[SerializeField] private AddressableAudioSource dippingSound;
		[SerializeField] private ItemTrait mopTrait;
		[SerializeField] private ItemTrait trashbagTrait;

		private ItemStorage jaintorToolsHolding;

		private void Awake()
		{
			jaintorToolsHolding = GetComponent<ItemStorage>();
		}

		public new bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public new void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.IsAltClick || interaction.Intent == Intent.Disarm)
			{
				if (interaction.HandObject == null)
				{
					Inventory.ServerTransfer(jaintorToolsHolding.GetTopOccupiedIndexedSlot(), interaction.HandSlot);
					return;
				}
				if (Inventory.ServerTransfer(interaction.HandObject.PickupableOrNull().ItemSlot,
					    jaintorToolsHolding.GetNextFreeIndexedSlot()))
				{
					Chat.AddLocalMsgToChat($"{interaction.PerformerPlayerScript.visibleName} adds a {interaction.HandObject.ExpensiveName()} to the {gameObject.ExpensiveName()}", interaction.Performer);
					return;
				}
				Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} has no space for the {interaction.HandObject.ExpensiveName()}");
				Logger.Log("No space found in cart");
				return;
			}
			if(interaction.HandObject == null) return;
			Logger.Log("Player has item in his hand");
			if (interaction.HandObject.Item().HasTrait(mopTrait))
			{
				base.ServerPerformInteraction(interaction);
				if(dippingSound != null) _ = SoundManager.PlayNetworkedAtPosAsync(dippingSound, gameObject.AssumedWorldPosServer());
				return;
			}
			foreach (var slot in jaintorToolsHolding.GetItemSlots())
			{
				if(slot.IsEmpty) continue;
				if(slot.ItemAttributes.HasTrait(trashbagTrait) == false) continue;
				if(slot.ItemObject.TryGetComponent<ItemStorage>(out var bag) == false) continue;
				if (bag.ServerTryTransferFrom(interaction.HandSlot))
				{
					Chat.AddLocalMsgToChat($"{interaction.PerformerPlayerScript.visibleName} throws a {interaction.HandObject.ExpensiveName()} into the {gameObject.ExpensiveName()}", interaction.Performer);
					return;
				}
				Chat.AddExamineMsg(interaction.Performer, "This wont fit into the trash bag that's on this cart!");
			}
		}
	}
}