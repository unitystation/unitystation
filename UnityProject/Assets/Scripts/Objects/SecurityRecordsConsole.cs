using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Objects.Security
{
	public class SecurityRecordsConsole : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private ItemStorage itemStorage;
		private ItemSlot itemSlot;

		public IDCard IdCard => itemSlot.Item != null ? itemSlot.Item.GetComponent<IDCard>() : null;
		public SecurityRecordsUpdateEvent OnConsoleUpdate = new SecurityRecordsUpdateEvent();

		private void Awake()
		{
			//we can just store a single card.
			itemStorage = GetComponent<ItemStorage>();
			itemSlot = itemStorage.GetIndexedItemSlot(0);
			itemSlot.OnSlotContentsChangeServer.AddListener(OnServerSlotContentsChange);
		}

		private void OnServerSlotContentsChange()
		{
			//propagate the ID change to listeners
			OnConsoleUpdate.Invoke();
		}

		private ItemSlot GetBestSlot(GameObject item, ConnectedPlayer subject)
		{
			if (subject == null)
			{
				return default;
			}

			var playerStorage = subject.Script.DynamicItemStorage;
			return playerStorage.GetBestHandOrSlotFor(item);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side))
				return false;

			//interaction only works if using an ID card on console
			if (!Validations.HasComponent<IDCard>(interaction.HandObject))
				return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//Eject existing id card if there is one and put new one in
			if (itemSlot.Item != null)
			{
				ServerRemoveIDCard(interaction.PerformerPlayerScript.connectedPlayer);
			}

			Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
		}

		/// <summary>
		/// Spits out ID card from console and updates login details.
		/// </summary>
		public void ServerRemoveIDCard(ConnectedPlayer player)
		{
			if (!Inventory.ServerTransfer(itemSlot, GetBestSlot(itemSlot.ItemObject, player)))
			{
				Inventory.ServerDrop(itemSlot);
			}
		}
	}

	public class SecurityRecordsUpdateEvent : UnityEvent { }
}
