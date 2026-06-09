using UnityEngine;
using UnityEngine.Events;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items;
using US13.Managers;
using US13.Systems.Clearance;
using US13.Systems.Inventory;

namespace US13.Objects
{
	public class SecurityRecordsConsole : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private ItemStorage itemStorage;
		private ItemSlot itemSlot;

		public IDCard IdCard => itemSlot.Item != null ? itemSlot.Item.GetComponent<IDCard>() : null;
		public SecurityRecordsUpdateEvent OnConsoleUpdate = new SecurityRecordsUpdateEvent();
		public ClearanceRestricted Restricted { get; private set; }

		private void Awake()
		{
			//we can just store a single card.
			itemStorage = GetComponent<ItemStorage>();
			itemSlot = itemStorage.GetIndexedItemSlot(0);
			itemSlot.OnSlotContentsChangeServer.AddListener(OnServerSlotContentsChange);
			Restricted = GetComponent<ClearanceRestricted>();
		}

		private void OnServerSlotContentsChange()
		{
			//propagate the ID change to listeners
			OnConsoleUpdate.Invoke();
		}

		private ItemSlot GetBestSlot(GameObject item, PlayerInfo subject)
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
			if (DefaultWillInteract.Default(interaction, side) == false)
				return false;

			//interaction only works if using an ID card on console
			if (Validations.HasComponent<IDCard>(interaction.HandObject) == false && interaction.IsAltClick == false)
				return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//Eject existing id card if there is one and put new one in
			if (itemSlot.Item != null)
			{
				ServerRemoveIDCard(interaction.PerformerPlayerScript.PlayerInfo);
			}

			if (interaction.IsAltClick) return;

			Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
		}

		/// <summary>
		/// Spits out ID card from console and updates login details.
		/// </summary>
		public void ServerRemoveIDCard(PlayerInfo player)
		{
			if (!Inventory.ServerTransfer(itemSlot, GetBestSlot(itemSlot.ItemObject, player)))
			{
				Inventory.ServerDrop(itemSlot);
			}
		}
	}

	public class SecurityRecordsUpdateEvent : UnityEvent { }
}
