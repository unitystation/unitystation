﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class InventoryManager : MonoBehaviour
{
	private static InventoryManager inventoryManager;

	public static InventoryManager Instance
	{
		get
		{
			if (!inventoryManager)
			{
				inventoryManager = FindObjectOfType<InventoryManager>();
			}
			return inventoryManager;
		}
	}

	//Clientside only:
	public static List<InventorySlot> AllClientInventorySlots = new List<InventorySlot>();
	//Server holds all slots for all the clients:
	public static List<InventorySlot> AllServerInventorySlots = new List<InventorySlot>();

	void OnEnable()
	{
		SceneManager.activeSceneChanged += Instance.OnSceneChange;
	}

	void OnDisable()
	{
		SceneManager.activeSceneChanged -= Instance.OnSceneChange;
	}

	public void OnSceneChange(Scene lastScene, Scene newScene)
	{
		AllClientInventorySlots.Clear();
		AllServerInventorySlots.Clear();
	}

	public static void AddSlot(InventorySlot slot, bool isServer)
	{
		if (isServer)
		{
			AllServerInventorySlots.Add(slot);
		}
		else
		{
			AllClientInventorySlots.Add(slot);
		}
	}

	/// <summary>
	/// Updates the inventory slot list, transferring an item out of / into an inventory slot (or ground).
	/// </summary>
	/// <param name="isServer">whether to update this client's inventory slots or the server's inventory slots</param>
	/// <param name="UUID">UUID of the destination inventory slot, empty string if item is not going into an inventory slot (such as when dropping it)</param>
	/// <param name="item">gameobject representing the item being transferred</param>
	/// <param name="FromUUID">UUID of the previous inventory slot that item is coming from, empty string if it's not coming from
	/// another inventory slot</param>
	public static void UpdateInvSlot(bool isServer, string UUID, GameObject item, string FromUUID = "")
	{
		bool uiSlotChanged = false;
		string toSlotName = "";
		GameObject fromOwner = null;
		GameObject toOwner = null;

		//find the inventory slot with the given UUID
		var index = InventorySlotList(isServer).FindIndex(
			x => x.UUID == UUID);
		if (index != -1)
		{
			var invSlot = InventorySlotList(isServer)[index];
			//put the item in the slot
			invSlot.Item = item;
			if (invSlot.IsUISlot)
			{
				//if this is a UI slot, mark that it has been changed and keep track of the new owner so we can send the update
				//message
				uiSlotChanged = true;
				toSlotName = invSlot.SlotName;
				toOwner = invSlot.Owner.gameObject;
			}
		}
		if (!string.IsNullOrEmpty(FromUUID))
		{
			//if this came from another slot, find it
			index = InventorySlotList(isServer).FindIndex(
				x => x.UUID == FromUUID);
			if (index != -1)
			{
				var invSlot = InventorySlotList(isServer)[index];
				//remove the item from the previous slot
				invSlot.Item = null;
				if (invSlot.IsUISlot)
				{
					//if the previous slot had an owner, track the owner so we can include it in the update message
					uiSlotChanged = true;
					fromOwner = invSlot.Owner.gameObject;
				}
			}
		}

		//Only ever sync UI slots straight away, storage slots will sync when they are being observed (picked up and inspected)
		if (isServer && uiSlotChanged)
		{
			//send the update to the player who the item was taken from and the player it was
			//given to
			if (fromOwner != null)
			{
				UpdateSlotMessage.Send(fromOwner, UUID, FromUUID, item);
			}
			if (toOwner != null)
			{
				UpdateSlotMessage.Send(toOwner, UUID, FromUUID, item);
			}

		}

		if (!isServer && uiSlotChanged)
		{
			UIManager.UpdateSlot(new UISlotObject(UUID, item, FromUUID));
		}
	}

	/// <summary>
	/// To clear an Item from an inventory slot and place at HiddenPos
	/// (for cases where items are consumed while in a slot)
	/// Can only be called on the server
	/// </summary>
	/// <param name="slotToClear"></param>
	public static void DestroyItemInSlot(InventorySlot slotToClear)
	{
		if (slotToClear.Item == null)
		{
			//Slot is already empty
			return;
		}

		DropItem(slotToClear, TransformState.HiddenPos);
		//TODO When ItemFactory has been refactored in 0.4 then return items here to the pool
		//If they came from the poolmanager
	}

	//Server only
	/// <summary>
	/// To clear an Item from an inventory slot and place at HiddenPos
	/// (for cases where items are consumed while in a slot)
	/// Can only be called on the server
	/// </summary>
	/// <param name="item"></param>
	public static void DestroyItemInSlot(GameObject item)
	{
		if (item == null)
		{
			return;
		}

		var invSlot = GetSlotFromItem(item);
		if (invSlot != null)
		{
			DropItem(invSlot, TransformState.HiddenPos);
		}
		//TODO When ItemFactory has been refactored in 0.4 then return items here to the pool
		//If they came from the poolmanager
	}

	/// <summary>
	/// Get the slot ID from an Item.
	/// Returns null if item is not in a slot
	/// </summary>
	/// <param name="item"></param>
	/// <param name="isServer"></param>
	/// <returns></returns>
	public static string GetSlotIDFromItem(GameObject item, bool isServer = true)
	{
		string UUID = "";
		if (item == null)
		{
			return UUID;
		}

		UUID = GetSlotFromItem(item)?.UUID;
		return UUID;
	}

	/// <summary>
	/// Get an Inventory slot from an Item
	/// Returns null if the item is not in a slot
	/// </summary>
	/// <param name="item"></param>
	/// <param name="isServer"></param>
	/// <returns></returns>
	public static InventorySlot GetSlotFromItem(GameObject item, bool isServer = true)
	{
		InventorySlot slot = null;
		if (item == null)
		{
			return slot;
		}
		var index = InventorySlotList(isServer).FindLastIndex(x => x.Item == item);
		if (index != -1)
		{
			slot = InventorySlotList(isServer)[index];
		}
		return slot;
	}

	/// <summary>
	/// Get an Inventoryslot from a slot Unique ID
	/// Returns null if UUID is invalid or not found
	/// </summary>
	/// <param name="UUID"></param>
	/// <param name="isServer"></param>
	/// <returns></returns>
	public static InventorySlot GetSlotFromUUID(string UUID, bool isServer)
	{
		InventorySlot slot = null;
		var index = InventorySlotList(isServer).FindLastIndex(x => x.UUID == UUID);
		if (index != -1)
		{
			slot = InventorySlotList(isServer)[index];
		}
		return slot;
	}

	/// <summary>
	/// Get Unique ID of a slot from a clients UI slot name (i.e. belt)
	/// </summary>
	/// <param name="slotName"></param>
	/// <returns></returns>
	public static string GetClientUUIDFromSlotName(string slotName)
	{
		string uuid = "";
		var index = AllClientInventorySlots.FindLastIndex(x => x.SlotName == slotName);
		if (index != -1)
		{
			uuid = AllClientInventorySlots[index].UUID;
		}
		return uuid;
	}

	//Server only:
	/// <summary>
	/// Get an Inventory slot from originators hand id (i.e leftHand)
	/// Can only be used ont he server
	/// </summary>
	/// <param name="originator"></param>
	/// <param name="hand"></param>
	/// <returns></returns>
	public static InventorySlot GetSlotFromOriginatorHand(GameObject originator, string hand)
	{
		InventorySlot slot = null;

		var index = AllServerInventorySlots.FindLastIndex(x => x.Owner != null && x.Owner.gameObject == originator && x.SlotName == hand);
		if (index != -1)
		{
			slot = AllServerInventorySlots[index];
		}

		return slot;
	}

	private static List<InventorySlot> InventorySlotList(bool isServer)
	{
		if (isServer)
		{
			return AllServerInventorySlots;
		}
		return AllClientInventorySlots;
	}

	private static void DropItem(InventorySlot slot, Vector3 dropPos)
	{
		slot.Item?.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
		var objTransform = slot.Item.GetComponent<CustomNetTransform>();
		if (dropPos != TransformState.HiddenPos)
		{
			if (slot.Owner != null)
			{
				//Inertia drop works only if player has external impulse (space floating etc.)
				objTransform.InertiaDrop(dropPos, slot.Owner.PlayerSync.SpeedServer, slot.Owner.PlayerSync.ServerImpulse);
			}
			else
			{
				objTransform.AppearAtPositionServer(dropPos);
			}
		}
		ObjectBehaviour itemObj = slot.Item.GetComponent<ObjectBehaviour>();
		if (itemObj)
		{
			itemObj.parentContainer = null;
		}
		slot.Item.GetComponent<RegisterTile>().UpdatePosition();
		UpdateInvSlot(true, "", slot.Item, slot.UUID);
	}

	//Server only
	/// <summary>
	/// Drop an item from a slot at a given position
	/// Use only on the server. Results are synced with clients
	/// </summary>
	/// <param name="player"></param>
	/// <param name="item"></param>
	/// <param name="pos"></param>
	public static void DropGameItem(GameObject player, GameObject item, Vector3 pos)
	{
		if (!item)
		{
			Logger.LogWarning("Trying to drop null object", Category.Inventory);
			return;
		}
		NetworkIdentity networkIdentity = player.GetComponent<NetworkIdentity>();
		if (!networkIdentity)
		{
			Logger.LogWarning("Unable to drop as NetIdentity is gone", Category.Equipment);
			return;
		}

		DropItem(GetSlotFromItem(item), pos);
	}

	/// <summary>
	/// Performs cleanup needed when a player disconnects. Drops their items at their current location and removes all the inventory slots.
	/// </summary>
	/// <param name="owner">gameobject of the player that is disconnecting, which should still be present wherever it was prior to disconnecting.</param>
	public static void HandleDisconnect(GameObject owner)
	{
		//drop everything
		AllServerInventorySlots
			.FindAll(x => x.Owner && x.Owner.gameObject == owner && x.Item)
			.ForEach(x => DropGameItem(owner, x.Item, owner.transform.position));

		//remove their slots
		AllServerInventorySlots.RemoveAll(x => x.Owner && x.Owner.gameObject == owner);
	}
}

//Helps identify the position in syncEquip list
public enum Epos
{
	suit,
	belt,
	head,
	feet,
	face,
	mask,
	uniform,
	leftHand,
	rightHand,
	eyes,
	back,
	hands,
	ear,
	neck,
	id,
	storage01,
	storage02,
	suitStorage
}