﻿using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	/// Server tells client the current status of a particular item slot.
	/// </summary>
	public class UpdateItemSlotMessage : ServerMessage<UpdateItemSlotMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Storage;
			public uint Item;
			public int SlotIndex;
			public NamedSlot NamedSlot;
		}

		public override void Process(NetMessage msg)
		{
			//server calls their own client side hooks, so server doesn't do anything here.
			//It's necessary for it to be this way because by the time the server reaches this point,
			//the change to this slot has already occurred so it can't figure out what the previous
			//slot was for this item.
			if (!CustomNetworkManager.IsServer)
			{
				LoadMultipleObjects(new uint[]{msg.Storage, msg.Item});
				if (NetworkObjects[0] == null) return;

				ItemSlot slot = null;
				if (msg.SlotIndex == -1)
				{
					slot = ItemSlot.GetNamed(NetworkObjects[0].GetComponent<ItemStorage>(), msg.NamedSlot);
				}
				else
				{
					slot = ItemSlot.GetIndexed(NetworkObjects[0].GetComponent<ItemStorage>(), msg.SlotIndex);
				}

				var previouslyInSlot = slot.ItemObject;
				var pickupable = msg.Item == NetId.Invalid ? null : NetworkObjects[1].GetComponent<Pickupable>();
				slot.ClientUpdate(pickupable);


				if (previouslyInSlot != null)
				{
					if (pickupable != null)
					{
						//was removed from slot
						pickupable._SetItemSlot(null);
					}

					var moveInfo = ClientInventoryMove.OfType(ClientInventoryMoveType.Removed);
					var hooks = previouslyInSlot.GetComponents<IClientInventoryMove>();
					foreach (var hook in hooks)
					{
						hook.OnInventoryMoveClient(moveInfo);
					}
				}

				if (pickupable != null)
				{
					//was added to slot
					pickupable._SetItemSlot(slot);
					var moveInfo = ClientInventoryMove.OfType(ClientInventoryMoveType.Added);
					var hooks = pickupable.GetComponents<IClientInventoryMove>();
					foreach (var hook in hooks)
					{
						hook.OnInventoryMoveClient(moveInfo);
					}
				}
			}
		}

		/// <summary>
		/// Inform the client about this slot's current status.
		/// </summary>
		/// <param name="recipient">client to inform</param>
		/// <param name="inventorySlot">slot to tell them about</param>
		/// <param name="informEmpty">if true, regardless of the slot's actual status, it
		/// will be reported to the client as empty.</param>
		/// <returns></returns>
		public static void Send(GameObject recipient, ItemSlot itemSlot, bool informEmpty = false)
		{
			NetMessage msg = new NetMessage
			{
				Storage = itemSlot.ItemStorageNetID,
				Item = informEmpty ? NetId.Invalid : (itemSlot.Item != null ? itemSlot.Item.GetComponent<NetworkIdentity>().netId : NetId.Invalid),
				SlotIndex = itemSlot.SlotIdentifier.SlotIndex,
				NamedSlot = itemSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.none)
			};

			SendTo(recipient, msg);
		}

		/// <summary>
		/// Inform the clients about this slot's current status.
		/// </summary>
		/// <param name="recipients">clients to inform</param>
		/// <param name="inventorySlot">slot to tell them about</param>
		/// <returns></returns>
		public static void Send(IEnumerable<GameObject> recipients, ItemSlot itemSlot)
		{
			NetMessage msg = new NetMessage
			{
				Storage = itemSlot.ItemStorageNetID,
				Item = itemSlot.Item != null ? itemSlot.Item.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
				SlotIndex = itemSlot.SlotIdentifier.SlotIndex,
				NamedSlot = itemSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.none)
			};

			foreach (var recipient in recipients)
			{
				SendTo(recipient, msg);
			}
		}
	}
}