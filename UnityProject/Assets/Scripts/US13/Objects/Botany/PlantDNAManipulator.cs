using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items.Botany;
using US13.Systems.Inventory;
using US13.UI.Objects.Botany.PlantDNAManipulator;
using Util;

namespace US13.Objects.Botany
{
	public class PlantDNAManipulator : MonoBehaviour,  ICheckedInteractable<HandApply>
	{

		public ItemStorage ItemStorage;

		public PlantDNADataDisc PlantDNADataDisc => ItemStorage.GetIndexedItemSlot(0)?.Item?.GetComponent<PlantDNADataDisc>();
		public SeedPacket SeedPacket => ItemStorage.GetIndexedItemSlot(1)?.Item?.GetComponent<SeedPacket>();

		private GUI_PlantDNAManipulator GUI_PlantDNAManipulator;

		public void UpdateDisplay()
		{
			GUI_PlantDNAManipulator.OrNull()?.UpdateDisplay();
		}

		public void RegisterConsoleGUI(GUI_PlantDNAManipulator GUI_PlantDNAManipulator)
		{
			if (GUI_PlantDNAManipulator.IsMasterTab == false) return;
			this.GUI_PlantDNAManipulator = GUI_PlantDNAManipulator;
		}


		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.UsedObject == null) return false;

			if (Validations.HasComponent<PlantDNADataDisc>(interaction.UsedObject) == false
			    && Validations.HasComponent<SeedPacket>(interaction.UsedObject) == false) return false;

			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var PDNA = interaction.UsedObject.GetComponent<PlantDNADataDisc>();
			if (PDNA != null)
			{
				if (PlantDNADataDisc)
				{
					Inventory.ServerSwap(interaction.HandSlot, ItemStorage.GetIndexedItemSlot(0));
				}
				else
				{
					Inventory.ServerTransfer(interaction.HandSlot, ItemStorage.GetIndexedItemSlot(0));
				}

			}
			var Seedp = interaction.UsedObject.GetComponent<SeedPacket>();
			if (Seedp != null)
			{

				if (SeedPacket)
				{
					Inventory.ServerSwap(interaction.HandSlot, ItemStorage.GetIndexedItemSlot(1));
				}
				else
				{
					Inventory.ServerTransfer(interaction.HandSlot, ItemStorage.GetIndexedItemSlot(1));
				}

			}

			UpdateDisplay();
		}

	}
}
