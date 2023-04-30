using System.Collections.Generic;
using System.Linq;
using Chemistry.Components;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace Objects.Other
{
	public class JanitorCart : ReagentContainer, ICheckedInteractable<HandApply>, IHoverTooltip
	{
		[SerializeField] private SpriteHandler waterSpriteHandler;
		[SerializeField] private SpriteDataSO waterSpriteSO;
		[SerializeField] private ItemTrait mopTrait;
		[SerializeField] private ItemTrait trashbagTrait;

		private ItemStorage jaintorToolsHolding;
		private IHoverTooltip hoverTooltipImplementation;

		private void Awake()
		{
			jaintorToolsHolding = GetComponent<ItemStorage>();
		}

		private void Start()
		{
			//Do this on start because it really pisses off the game when scenes are loading.
			CheckSpriteStatus();
		}

		public new bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public new void ServerPerformInteraction(HandApply interaction)
		{
			CheckSpriteStatus();
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
				Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} grabs " +
																$"something from the {gameObject.ExpensiveName()}.");
				return true;
			}

			return false;
		}

		private void AddGarbageBag(HandApply interaction)
		{
			if (Inventory.ServerTransfer(interaction.HandObject.PickupableOrNull().ItemSlot,
				    jaintorToolsHolding.GetNextFreeIndexedSlot()))
			{
				Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} adds a {interaction.HandObject.ExpensiveName()} to the {gameObject.ExpensiveName()}.");
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
					Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} throws a {interaction.HandObject.ExpensiveName()} into the {gameObject.ExpensiveName()}.");
					return;
				}
				Chat.AddExamineMsg(interaction.Performer, "This wont fit into the trash bag that's on this cart!");
			}
		}

		private void MopInteraction(HandApply interaction)
		{
			if (interaction.Intent == Intent.Disarm)
			{
				if (jaintorToolsHolding.GetNextEmptySlot() == null) return;
				if (jaintorToolsHolding.ServerTryTransferFrom(interaction.HandObject) == false)
				{
					Chat.AddExamineMsg(interaction.Performer, "You can't add this to the cart");
					return;
				}
				Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} adds a {interaction.HandObject.ExpensiveName()} to the {gameObject.ExpensiveName()}.");
				return;
			}
			base.ServerPerformInteraction(interaction);
		}

		private void CheckSpriteStatus()
		{
			if (IsEmpty)
			{
				waterSpriteHandler.PushClear();
			}
			else
			{
				waterSpriteHandler.SetSpriteSO(waterSpriteSO);
			}
		}

		private List<TextColor> CheckHandTips()
		{
			var slots = PlayerManager.Equipment.ItemStorage.GetHandSlots();
			if (slots is null) return null;
			var interactionList = new List<TextColor>();
			foreach (var hand in slots)
			{
				if (hand.IsEmpty) continue;
				if (hand.ItemAttributes.HasTrait(mopTrait))
				{
					interactionList.Add(new TextColor(){Color = Color.green, Text = "Click with a mop to fill it with water."});
				}

				if (hand.ItemAttributes.HasTrait(CommonTraits.Instance.Trash))
				{
					interactionList.Add(new TextColor(){Color = Color.green, Text = "Click with trash in hand to throw trash in garbage bag if available in cart."});
				}

				if (hand.ItemAttributes.HasTrait(CommonTraits.Instance.Bucket))
				{
					interactionList.Add(new TextColor(){Color = Color.green, Text = "Click with bucket to fill cart with liquid."});
				}
			}

			return interactionList;
		}

		public string HoverTip()
		{
			var status = "";
			if (ReagentMixTotal >= MaxCapacity) status = "is full.";
			if (ReagentMixTotal <= MaxCapacity) status = "is almost full.";
			if (ReagentMixTotal <= MaxCapacity / 2) status = "is half full.";
			if (IsEmpty) status = "is empty.";
			return $"It appears that it {status}";
		}

		public string CustomTitle() { return null; }

		public Sprite CustomIcon() { return null; }

		public List<Sprite> IconIndicators() { return null; }

		public List<TextColor> InteractionsStrings()
		{
			var interactions = new List<TextColor>();
			interactions.Add(new TextColor(){Color = Color.cyan, Text = "Click with an empty hand to add or remove mops with disarm intent."});
			interactions.Add(new TextColor(){Color = Color.green, Text = "Click with an empty hand to add or remove garbage bags."});
			var handInteractions = CheckHandTips();
			// "is {}" is a null check. Same as saying "is not null" but I'm not sure if unity can handle that syntax sugar.
			if (handInteractions is {}) interactions.AddRange(handInteractions);
			return interactions;
		}
	}
}