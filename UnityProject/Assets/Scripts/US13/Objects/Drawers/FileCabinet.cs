using System.Collections.Generic;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items.Traits;
using US13.Systems.Inventory;
using Util;

namespace US13.Objects.Drawers
{
	public class FileCabinet : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private ItemStorage itemStorage;

		[SerializeField] private List<ItemTrait> acceptableObjects = new List<ItemTrait>();

		private void Awake()
		{
			itemStorage = GetComponent<ItemStorage>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject != null && interaction.HandObject.PickupableOrNull() != null && interaction.HandObject.Item().HasAnyTrait(acceptableObjects))
			{
				Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} adds the " +
																$"{interaction.HandObject.ExpensiveName()} to the {gameObject.ExpensiveName()}.");
				Inventory.ServerTransfer(interaction.HandObject.PickupableOrNull().ItemSlot,
					itemStorage.GetNextFreeIndexedSlot());
				return;
			}
			itemStorage.ServerDropAll();
			Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} opens the {gameObject.ExpensiveName()}.");
		}
	}
}