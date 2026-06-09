using Logs;
using UnityEngine;
using US13.Core.Chat;
using US13.Systems.Inventory;
using Util;

namespace US13.Items
{
	/// <summary>
	/// Component that houses functionality for the OnDrop and OnThrow events to hook onto via the unity inspector,
	/// or dynamically. Does not do anything on its own and requires the OnDropOrThrow function to be subscribed to an event.
	/// Re-adds this item back to an item storage when dropped, mainly used for things like the Defib Paddles.
	/// </summary>
	public class AddBackToStorageOnDropOrThrow : MonoBehaviour, IServerInventoryMove
	{
		[SerializeField] private ItemStorage storage;
		[SerializeField] private string OnAddBackMessage = "The paddles spring back into its storage unit.";

		private void Start()
		{
			if (storage == null) Setup();
		}

		private void Setup()
		{
			var itemSlot = gameObject.PickupableOrNull()?.ItemSlot;
			if (itemSlot == null)
			{
				Loggy.Error($"No ItemSlot defined on {gameObject.name} for AddBackToStorageOnDropOrThrow");
				return;
			}
			storage = itemSlot.ItemStorage;
			if (storage == null)
			{
				Loggy.Error($"No ItemStorage defined on {gameObject.name} for AddBackToStorageOnDropOrThrow.");
			}
		}

		void IServerInventoryMove.OnInventoryMoveServer(InventoryMove info)
		{
			if (storage == null)
			{
				Setup();
				return;
			}

			if (info?.MovedObject.OrNull()?.gameObject != this.gameObject || info?.ToSlot?.ItemStorage == storage) return;
			if (info?.ToSlot?.NamedSlot is not (NamedSlot.leftHand or NamedSlot.rightHand))
			{
				if (storage.ServerTryAdd(gameObject))
				{
					Chat.AddActionMsgToChat(gameObject, OnAddBackMessage);
				}
				else
				{
					Loggy.Error($"Something went wrong while trying to re-add this item back to their item storage on {gameObject.name}.\n {InventoryMove.ToString(info)}");
				}
			}
		}
	}
}