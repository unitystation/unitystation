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

	public static void UpdateInvSlot(bool isServer, string UUID, GameObject item, string FromUUID = "")
	{
		bool uiSlotChanged = false;
		string toSlotName = "";
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
					owner = invSlot.Owner.gameObject;
				}
			}
		}

		//Only ever sync UI slots straight away, storage slots will sync when they are being observed (picked up and inspected)
		if (isServer && uiSlotChanged)
		{
			UpdateSlotMessage.Send(owner, UUID, FromUUID, item);
		}

		if (!isServer && uiSlotChanged)
		{
			UIManager.UpdateSlot(new UISlotObject(UUID, item, FromUUID));
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
	public static InventorySlot GetSlotFromOriginatorHand(GameObject originator, string hand)
	{
		InventorySlot slot = null;

		var index = AllServerInventorySlots.FindLastIndex(x => x.Owner?.gameObject == originator && x.SlotName == hand);
		if(index != -1){
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

	//Server only
	public static void DisposeItemServer(GameObject item)
	{
		DropItem(GetSlotFromItem(item), TransformState.HiddenPos);
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
				objTransform.InertiaDrop(dropPos, slot.Owner.PlayerSync.MoveSpeedServer, slot.Owner.PlayerSync.ServerImpulse);
			}
			else
			{
				objTransform.AppearAtPositionServer(dropPos);
			}
		}
		slot.Item.GetComponent<RegisterTile>().UpdatePosition();
		UpdateInvSlot(true, "", slot.Item, slot.UUID);
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