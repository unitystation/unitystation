using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.HealthV2.Living;
using US13.Items.Food;
using US13.Systems.Inventory;
using Util;

namespace US13.Items.Storage
{
	public class PillBottle : MonoBehaviour, ICheckedInteractable<HandApply>
	{

		private ItemStorage ItemStorage;

		private void Awake()
		{
			ItemStorage = this.GetComponentCustom<ItemStorage>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.IsAltClick) return false;
			if (Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject) == false) return false;

			if (side == NetworkSide.Server)
			{
				if (ItemStorage.HasAnyOccupied() == false) return false;
			}

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var pill = ItemStorage.GetFirstOccupiedSlot();
			var PillEdible = pill.Item.GetComponentCustom<Edible>();
			PillEdible.ServerPerformInteraction(interaction);
		}



	}
}
