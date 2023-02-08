using System;
using System.Linq;
using AddressableReferences;
using Chemistry.Components;
using Items;
using UnityEngine;

namespace Objects.Other
{
	public class JanitorCart : ReagentContainer, ICheckedInteractable<HandApply>
	{
		[SerializeField] private AddressableAudioSource dippingSound;
		[SerializeField] private SpriteHandler waterSprite;
		[SerializeField] private ItemTrait mopTrait;
		[SerializeField] private ItemTrait trashbagTrait;

		private ItemStorage jaintorToolsHolding;

		private void Awake()
		{
			jaintorToolsHolding = GetComponent<ItemStorage>();
			CheckSpriteStatus();
		}

		public new bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public new void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null)
			{
				if (GrabTool(interaction, mopTrait))      return;
				if (GrabTool(interaction, trashbagTrait)) return;
				return;
			}

			if (interaction.HandSlot.ItemAttributes.GetTraits().Contains(trashbagTrait))
			{
				AddGarbageBag(interaction);
				return;
			}

			if (interaction.HandObject.Item().HasTrait(mopTrait))
			{
				MopInteraction(interaction);
				return;
			}

			AddItemToGarbageBag(interaction);
		}

		private bool GrabTool(HandApply interaction, ItemTrait thingToGrab)
		{
			foreach (var slot in jaintorToolsHolding.GetIndexedSlots())
			{
				if(slot.IsEmpty) continue;
				if(slot.ItemAttributes.GetTraits().Contains(thingToGrab) == false) continue;
				if (Inventory.ServerTransfer(slot, interaction.HandSlot) == false) continue;
				Chat.AddLocalMsgToChat($"{interaction.PerformerPlayerScript.visibleName} grabs the " +
				                       $"something from the {gameObject.ExpensiveName()}", interaction.Performer);
				return true;
			}

			return false;
		}

		private void AddGarbageBag(HandApply interaction)
		{
			if (Inventory.ServerTransfer(interaction.HandObject.PickupableOrNull().ItemSlot,
				    jaintorToolsHolding.GetNextFreeIndexedSlot()))
			{
				Chat.AddLocalMsgToChat($"{interaction.PerformerPlayerScript.visibleName} adds a {interaction.HandObject.ExpensiveName()} to the {gameObject.ExpensiveName()}", interaction.Performer);
				return;
			}
			Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} has no space for the {interaction.HandObject.ExpensiveName()}");
		}

		private void AddItemToGarbageBag(HandApply interaction)
		{
			foreach (var slot in jaintorToolsHolding.GetItemSlots())
			{
				if (slot.IsEmpty) continue;
				if (slot.ItemAttributes.HasTrait(trashbagTrait) == false) continue;
				if (slot.ItemObject.TryGetComponent<ItemStorage>(out var bag) == false) continue;
				if (bag.ServerTryTransferFrom(interaction.HandSlot))
				{
					Chat.AddLocalMsgToChat($"{interaction.PerformerPlayerScript.visibleName} throws a {interaction.HandObject.ExpensiveName()} into the {gameObject.ExpensiveName()}", interaction.Performer);
					return;
				}
				Chat.AddExamineMsg(interaction.Performer, "This wont fit into the trash bag that's on this cart!");
			}
		}

		private void MopInteraction(HandApply interaction)
		{
			if (interaction.IsAltClick || interaction.Intent == Intent.Disarm)
			{
				if (jaintorToolsHolding.GetNextEmptySlot() == null) return;
				if (jaintorToolsHolding.ServerTransferGameObjectToItemSlot(interaction.HandObject, jaintorToolsHolding.GetNextEmptySlot()) == false) return;
				Chat.AddLocalMsgToChat($"{interaction.PerformerPlayerScript.visibleName} adds a {interaction.HandObject.ExpensiveName()} to the {gameObject.ExpensiveName()}", interaction.Performer);
				return;
			}
			base.ServerPerformInteraction(interaction);
			if (dippingSound != null) _ = SoundManager.PlayNetworkedAtPosAsync(dippingSound, gameObject.AssumedWorldPosServer());
			CheckSpriteStatus();
		}

		private void CheckSpriteStatus()
		{
			if (ReagentMixTotal.Approx(0))
			{
				waterSprite.PushClear();
			}
			else
			{
				waterSprite.ChangeSprite(0);
			}
		}
	}
}