using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	/// <summary>
	/// Clears the slot and sends network messages to update character sprites and the player's UI
	/// </summary>
	public static void ClearInvSlot(InventorySlot inventorySlot)
	{
		if (inventorySlot.IsUISlot)
		{
			UpdateSlotMessage.Send(inventorySlot.Owner, inventorySlot.Item, true, inventorySlot.equipSlot);

			if(IsEquipSpriteSlot(inventorySlot.equipSlot))
			{
				EquipmentSpritesMessage.SendToAll(inventorySlot.Owner, (int)inventorySlot.equipSlot, null);
			}
		}
		inventorySlot.Item = null;
	}

	public static void ClientClearInvSlot(PlayerNetworkActions pna, EquipSlot equipSlot)
	{
		var inventorySlot = pna.Inventory[equipSlot];
		inventorySlot.Item = null;
		var UIitemSlot = InventorySlotCache.GetSlotByEvent(inventorySlot.equipSlot);
		UIitemSlot.Clear();
	}


	/// <summary>
	/// Sets the item to the slot and sends network messages to update character sprites and the player's UI
	/// </summary>
	public static void EquipInInvSlot(InventorySlot inventorySlot, GameObject item)
	{
		if (inventorySlot.IsUISlot)
		{
			UpdateSlotMessage.Send(inventorySlot.Owner, item, false, inventorySlot.equipSlot);

			if (IsEquipSpriteSlot(inventorySlot.equipSlot))
			{
				EquipmentSpritesMessage.SendToAll(inventorySlot.Owner, (int)inventorySlot.equipSlot, item);

			}
		}
		inventorySlot.Item = item;

	}

	public static void ClientEquipInInvSlot(PlayerNetworkActions pna, GameObject item, EquipSlot equipSlot)
	{
		var inventorySlot = pna.Inventory[equipSlot];
		inventorySlot.Item = item;
		var UIitemSlot = InventorySlotCache.GetSlotByEvent(inventorySlot.equipSlot);
		UIitemSlot.SetItem(item);
	}

	private static bool IsEquipSpriteSlot(EquipSlot equipSlot)
	{
		if (equipSlot == EquipSlot.id || equipSlot == EquipSlot.storage01 ||
			equipSlot == EquipSlot.storage02 || equipSlot == EquipSlot.suitStorage)
		{
			return false;
		}
		return true;
	}

	/// <summary>
	/// Get an Inventory slot from an Item
	/// Returns null if the item is not in a slot
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public static InventorySlot GetSlotFromItem(GameObject item, PlayerNetworkActions pna)
	{
		foreach (var slot in pna.Inventory)
		{
			if(item == slot.Value.Item)
			{
				return slot.Value;
			}
		}
		return null;
	}

	//Server only:
	/// <summary>
	/// Get an Inventory slot from originators hand id (i.e leftHand)
	/// Can only be used ont he server
	/// </summary>
	/// <param name="originator"></param>
	/// <param name="hand"></param>
	/// <returns></returns>
	public static InventorySlot GetSlotFromOriginatorHand(GameObject originator, EquipSlot hand)
	{
		var pna = originator.GetComponent<PlayerNetworkActions>();
		var slot = pna.Inventory[hand];
		return slot;
	}

	public static void DropItem(GameObject item, Vector3 dropPos, PlayerNetworkActions pna)
	{
		if (!item)
		{
			Logger.LogWarning("Trying to drop null object", Category.Inventory);
			return;
		}
		var slot = GetSlotFromItem(item, pna);
		item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
		var objTransform = item.GetComponent<CustomNetTransform>();
		if (dropPos != TransformState.HiddenPos)
		{
			if (slot.Owner != null)
			{
				//Inertia drop works only if player has external impulse (space floating etc.)
				var playerScript = slot.Owner.GetComponent<PlayerScript>();
				objTransform.InertiaDrop(dropPos, playerScript.PlayerSync.SpeedServer, playerScript.PlayerSync.ServerImpulse);
			}
			else
			{
				objTransform.AppearAtPositionServer(dropPos);
			}
		}
		ObjectBehaviour itemObj = item.GetComponent<ObjectBehaviour>();
		if (itemObj)
		{
			itemObj.parentContainer = null;
		}
		item.GetComponent<RegisterTile>().UpdatePositionServer();
		ClearInvSlot(slot);
	}
}

//Helps identify the position in syncEquip list
public enum EquipSlot
{
	exosuit,
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
	handcuffs,
	id,
	storage01,
	storage02,
	suitStorage,
	inventory01,
	inventory02,
	inventory03,
	inventory04,
	inventory05,
	inventory06,
	inventory07,


}