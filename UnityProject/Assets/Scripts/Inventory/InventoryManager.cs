using System;
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

	public static void UpdateInvSlot(bool isServer, string UUID, GameObject item, string FromUUID = "")
	{
		if(!isServer){
			Debug.Log("client rec: " + UUID + " item: " + item.name + " from: " + FromUUID);
		}
		bool uiSlotChanged = false;
		string toSlotName = "";
		string fromSlotName = "";
		GameObject owner = null;
		var index = InventorySlotList(isServer).FindIndex(
			x => x.UUID == UUID);
		if (index != -1)
		{
			var invSlot = InventorySlotList(isServer)[index];
			invSlot.Item = item;
			if (invSlot.IsUISlot)
			{
				uiSlotChanged = true;
				toSlotName = invSlot.SlotName;
				owner = invSlot.Owner.gameObject;
			}
		}
		if (!string.IsNullOrEmpty(FromUUID))
		{
			index = InventorySlotList(isServer).FindIndex(
				x => x.UUID == FromUUID);
			if (index != -1)
			{
				var invSlot = InventorySlotList(isServer)[index];
				invSlot.Item = null;
				if (invSlot.IsUISlot)
				{
					uiSlotChanged = true;
					fromSlotName = invSlot.SlotName;
					owner = invSlot.Owner.gameObject;
				}
			}
		}

		//Only ever sync UI slots straight away, storage slots will sync when they are being observed (picked up and inspected)
		if (isServer && uiSlotChanged)
		{
			Debug.Log("IS SERVER, AND SLOT CHANGED, SEND UPDATE SLOT MSG");
			UpdateSlotMessage.Send(owner, UUID, FromUUID, item);
		}

		if (!isServer && uiSlotChanged)
		{
			Debug.Log("IS SERVER: " + isServer + " UPDATE SLOT ON UIMANAGER: " + toSlotName + " With item: " + item.name);
			UIManager.UpdateSlot(new UISlotObject(toSlotName, item, fromSlotName));
		}
	}

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

	public static string GetClientUUIDFromSlotName(string slotName){
		string eventName = "";
		var index = AllClientInventorySlots.FindLastIndex(x => x.SlotName == slotName);
		if (index != -1)
		{
			eventName = AllClientInventorySlots[index].SlotName;
		}
		return eventName;
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
		var objTransform = slot.Item.GetComponent<CustomNetTransform>();
		if (slot.Owner != null)
		{
			//Inertia drop works only if player has external impulse (space floating etc.)
			objTransform.InertiaDrop(dropPos, slot.Owner.playerMove.speed, slot.Owner.PlayerSync.ServerState.Impulse);
		}
		else
		{
			objTransform.AppearAtPositionServer(dropPos);
		}
		UpdateInvSlot(true, "", slot.Item);
	}
	
	//Server only
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
		item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
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
	neck
}