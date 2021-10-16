using System;
using UnityEngine;
using Objects;

namespace Items.Cargo
{
	public class ExportScanner : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side))
			{
				return false;
			}

			if (interaction.HandObject != gameObject)
			{
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var (containedContents, price) = GetPrice(interaction.TargetObject);
			var exportName = interaction.TargetObject.ExpensiveName();
			var message = price > 0
				? $"Scanned { exportName }, value: { price } credits."
				: $"Scanned { exportName }, no export value.";

			if (containedContents)
			{
				message += " (contents included)";
			}

			Chat.AddExamineMsg(interaction.Performer, message);
		}

		private Tuple<bool, int> GetPrice(GameObject pricedObject)
		{
			var attributes = pricedObject.GetComponent<Attributes>();
			var price = attributes ? attributes.ExportCost : 0;

			var containedContents = false;
			var storage = pricedObject.GetComponent<InteractableStorage>();
			if (storage)
			{
				foreach (var slot in storage.ItemStorage.GetItemSlots())
				{
					if (!slot.Item)
					{
						continue;
					}

					containedContents = true;
					price += GetPrice(slot.Item.gameObject).Item2;
				}
			}

			if (pricedObject.TryGetComponent<ObjectContainer>(out var container))
			{
				foreach (var obj in container.GetStoredObjects())
				{
					containedContents = true;
					price += GetPrice(obj).Item2;
				}
			}

			return new Tuple<bool, int>(containedContents, price);
		}
	}
}
