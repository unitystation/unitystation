using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			bool isRemoving = interaction.HandObject == null;
			ItemSlot targetSlot = slots.FirstOrDefault(slot => isRemoving ? slot.Item != null : slot.Item == null);

			if (targetSlot != null)
			{
				ItemSlot from = isRemoving ? targetSlot : interaction.HandSlot;
				ItemSlot to = isRemoving ? interaction.HandSlot : targetSlot;
				Inventory.ServerTransfer(from, to);
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, $"It's {(isRemoving ? "Empty" : "Full")}");
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
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(ExamineText());
			if (canStoreOxygen == false || canStorePlasma == false)
			{
				stringBuilder.AppendLine($"It doesn't seem to be able to store {(canStoreOxygen ? "plasma" : "oxygen")} tanks.");
			}
			return stringBuilder.ToString();
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
			var list = new List<TextColor>
			{
				new() { Color = Color.green, Text = "Click on plasma/oxygen tank sprites to insert/remove that tank." },
				new() { Color = Color.green, Text = "Left Click with hand: Remove tank." },
				new() { Color = Color.green, Text = "Left Click with tank: Insert tank." }
			};
			return list;
		}
	}
}