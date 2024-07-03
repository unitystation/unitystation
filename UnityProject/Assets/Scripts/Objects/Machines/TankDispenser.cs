using System;
using System.Collections.Generic;
using Logs;
using Mirror;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;
using NaughtyAttributes;

namespace Objects.Machines
{
	[RequireComponent(typeof(ItemStorage))]
	public class TankDispenser : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>, IExaminable, IHoverTooltip {

		[SerializeField] private SpriteClickRegion plasmaTankRegion;
		[SerializeField] private SpriteHandler plasmaTankSpriteHandler;
		[SerializeField] private bool canStorePlasma;
		[SyncVar] private int plasmaCount;
		[ReadOnly] private List<ItemSlot> plasmaTankSlots = new(10);

		[SerializeField] private SpriteClickRegion oxygenTankRegion;
		[SerializeField] private SpriteHandler oxygenTankSpriteHandler;
		[SerializeField] private bool canStoreOxygen;
		[SyncVar] private int oxygenCount;
		[ReadOnly] private List<ItemSlot> oxygenTankSlots = new(10);

		private const int SPRITE_INDEX_MAX = 5;

		[SerializeField] private ItemStorage tankStorage;

		private void Start()
		{
			for (int i = 0; i < 10; i++)
			{
				oxygenTankSlots.Add(tankStorage.GetIndexedItemSlot(i));
				plasmaTankSlots.Add(tankStorage.GetIndexedItemSlot(i + 10));
			}
			UpdateSprite();
		}
		
		private void UpdateSprite()
		{
			if (isServer)
			{
				plasmaCount = GetFilledSlots(plasmaTankSlots);
				oxygenCount = GetFilledSlots(oxygenTankSlots);
				plasmaTankSpriteHandler.SetSpriteVariant(Math.Min(plasmaCount, SPRITE_INDEX_MAX));
				oxygenTankSpriteHandler.SetSpriteVariant(Math.Min(oxygenCount, SPRITE_INDEX_MAX));				
			}
		}

		private int GetFilledSlots(List<ItemSlot> slots)
		{
			int i = 0;
			foreach (var slot in slots)
			{
				if (slot.Item != null)
				{
					i++;
				}
			}
			return i;
		}

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return  Validations.HasItemTrait(interaction, CommonTraits.Instance.CanisterFillable) || interaction.HandObject == null;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if ((Validations.HasItemTrait(interaction, CommonTraits.Instance.CanisterFillable) || interaction.HandObject == null) == false) return;

			List<ItemSlot> slots = null;

			if (canStorePlasma && plasmaTankRegion.Contains(interaction.WorldPositionTarget))
			{
				slots = plasmaTankSlots;
			}
			else if (canStoreOxygen && oxygenTankRegion.Contains(interaction.WorldPositionTarget))
			{
				slots = oxygenTankSlots;
			}

			if (slots != null)
			{
				TankInteraction(slots, interaction);
			}
		}

		public void TankInteraction(List<ItemSlot> slots, PositionalHandApply interaction)
		{
			bool removing = interaction.HandObject == null;
			ItemSlot slot = null;

			foreach (var slotloop in slots)
			{
				if (removing ? slotloop.Item != null : slotloop.Item == null)
				{
					slot = slotloop;
					break;
				}
			}

			if (slot != null)
			{
				ItemSlot from = removing ? slot : interaction.HandSlot;
				ItemSlot to = removing ? interaction.HandSlot : slot;
				Inventory.ServerTransfer(from, to);
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, $"It's {(removing ? "Empty" : "Full")}");
			}
			UpdateSprite();
		}

		private string ExamineText()
		{
			if (canStoreOxygen && canStorePlasma)
			{
				return $"It has {plasmaCount} plasma tank{(plasmaCount > 1 ? "s" : "")} and {oxygenCount} oxygen tank{(oxygenCount > 1 ? "s" : "")} left.";
			}
			return $"It has {(canStoreOxygen ? $"{oxygenCount} oxygen" : $"{plasmaCount} plasma" )} tank{(canStoreOxygen ? oxygenCount > 1 ? "s" : "" : plasmaCount > 1 ? "s" : "")} left.";
		}

		public string Examine(Vector3 worldPos = default)
		{
			return ExamineText();
		}

		public string HoverTip()
		{
			return ExamineText();
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			return null;
		}
	}
}