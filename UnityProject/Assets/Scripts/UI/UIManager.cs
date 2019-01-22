using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	private static UIManager uiManager;
	public ControlAction actionControl;
	public DragAndDrop dragAndDrop;
	public ControlDisplays displayControl;
	public DisplayManager displayManager;
	public GameObject bottomBar;
	public Hands hands;
	public ControlIntent intentControl;
	public InventorySlotCache inventorySlotCache;
	public PlayerHealthUI playerHealthUI;
	public PlayerListUI playerListUIControl;
	public Text toolTip;
	public ControlWalkRun walkRunControl;
	public UI_StorageHandler storageHandler;
	public ZoneSelector zoneSelector;
	public bool ttsToggle;
	[HideInInspector]
	public ProgressBar progressBar;

	///Global flag for focused input field. Movement keystrokes are ignored if true.
	/// <see cref="InputFieldFocus"/> handles this flag automatically
	public static bool IsInputFocus
	{
		get
		{
			return Instance && Instance.isInputFocus;
		}
		set
		{
			if (!Instance)
			{
				return;
			}
			Instance.isInputFocus = value;
		}
	}

	public bool isInputFocus;

	public static UIManager Instance
	{
		get
		{
			if (!uiManager)
			{
				uiManager = FindObjectOfType<UIManager>();
			}

			return uiManager;
		}
	}

	//		public static ControlChat Chat => Instance.chatControl; //Use ChatRelay.Instance.AddToChatLog instead!
	public static ProgressBar ProgressBar => Instance.progressBar;
	public static PlayerHealthUI PlayerHealthUI => Instance.playerHealthUI;

	public static Hands Hands => Instance.hands;

	public static ControlIntent Intent => Instance.intentControl;

	public static ControlAction Action => Instance.actionControl;

	public static DragAndDrop DragAndDrop => Instance.dragAndDrop;

	public static ControlWalkRun WalkRun => Instance.walkRunControl;

	public static ControlDisplays Display => Instance.displayControl;

	public static PlayerListUI PlayerListUI => Instance.playerListUIControl;

	public static DisplayManager DisplayManager => Instance.displayManager;
	public static UI_StorageHandler StorageHandler => Instance.storageHandler;
	public static ZoneSelector ZoneSelector => Instance.zoneSelector;

	public static string SetToolTip
	{
		set { Instance.toolTip.text = value; }
	}

	public static InventorySlotCache InventorySlots => Instance.inventorySlotCache;

	/// <summary>
	///     Current Intent status
	/// </summary>
	public static Intent CurrentIntent { get; set; }

	/// <summary>
	///     What is DamageZoneSeclector currently set at
	/// </summary>
	public static BodyPartType DamageZone { get; set; }

	/// <summary>
	///     Is throw selected?
	/// </summary>
	public static bool IsThrow { get; set; }

	/// <summary>
	///     Is Oxygen On?
	/// </summary>
	public static bool IsOxygen { get; set; }

	public static void ResetAllUI()
	{
		UI_ItemSlot[] slots = Instance.GetComponentsInChildren<UI_ItemSlot>(true);
		foreach (UI_ItemSlot slot in slots)
		{
			slot.Reset();
		}
		foreach (DamageMonitorListener listener in Instance.GetComponentsInChildren<DamageMonitorListener>())
		{
			listener.Reset();
		}
		Camera2DFollow.followControl.ZeroStars();
	}

	/// <summary>
	///     use this for client UI mangling attepts
	/// </summary>
	public static bool TryUpdateSlot(UISlotObject slotInfo)
	{
		if (!CanPutItemToSlot(slotInfo))
		{
			return false;
		}
		InventoryInteractMessage.Send(slotInfo.SlotUUID, slotInfo.FromSlotUUID, slotInfo.SlotContents, true);
		UpdateSlot(slotInfo);
		return true;
	}

	/// <summary>
	///     rather direct method that doesn't check anything.
	///     probably should check if you CanPutItemToSlot before using it
	/// </summary>
	public static void UpdateSlot(UISlotObject slotInfo)
	{
		if (string.IsNullOrEmpty(slotInfo.SlotUUID) && !string.IsNullOrEmpty(slotInfo.FromSlotUUID))
		{
			//Dropping updates:
			var _fromSlot = InventorySlotCache.GetSlotByUUID(slotInfo.FromSlotUUID);
			if (_fromSlot != null)
			{
				CheckStorageHandlerOnMove(_fromSlot.Item);
				_fromSlot.Clear();
				return;
			}
		}
		//Logger.LogTraceFormat("Updating slots: {0}", Category.UI, slotInfo);
		//			InputTrigger.Touch(slotInfo.SlotContents);
		var slot = InventorySlotCache.GetSlotByUUID(slotInfo.SlotUUID);
		if (slot != null)
		{
			slot.SetItem(slotInfo.SlotContents);
		}

		var fromSlot = InventorySlotCache.GetSlotByUUID(slotInfo.FromSlotUUID);
		bool fromS = fromSlot != null;
		bool fromSI = fromSlot?.Item != null;

		if (fromSlot?.Item == slotInfo.SlotContents)
		{
			CheckStorageHandlerOnMove(fromSlot.Item);
			fromSlot.Clear();
		}
	}

	public static void CheckStorageHandlerOnMove(GameObject item)
	{
		if (item == null)
		{
			return;
		}
		var storageObj = item.GetComponent<StorageObject>();
		if (storageObj == null)
		{
			return;
		}
		if (storageObj == StorageHandler.storageCache)
		{
			StorageHandler.CloseStorageUI();
		}
	}

	public static bool CanPutItemToSlot(UISlotObject proposedSlotInfo)
	{
		if (proposedSlotInfo.IsEmpty() || !SendUpdateAllowed(proposedSlotInfo.SlotContents))
		{
			return false;
		}

		InventorySlot invSlot = InventoryManager.GetSlotFromUUID(proposedSlotInfo.SlotUUID, false);
		PlayerScript lps = PlayerManager.LocalPlayerScript;

		if (!lps || lps.canNotInteract() || invSlot.Item != null)
		{
			return false;
		}

		UI_ItemSlot uiItemSlot = InventorySlotCache.GetSlotByUUID(invSlot.UUID);
		if (uiItemSlot == null)
		{
			//Could it be a storage obj that is closed?
			ItemSize checkMaxSizeOfStorage;
			if (SlotIsFromClosedBag(invSlot, out checkMaxSizeOfStorage))
			{
				var itemAtts = proposedSlotInfo.SlotContents.GetComponent<ItemAttributes>();
				if (itemAtts != null)
				{
					if (itemAtts.size <= checkMaxSizeOfStorage)
					{
						return true;
					}
				}
			}

			return false;
		}

		if (!uiItemSlot.CheckItemFit(proposedSlotInfo.SlotContents))
		{
			return false;
		}
		return true;
	}

	private static bool SlotIsFromClosedBag(InventorySlot invSlot, out ItemSize slotMaxItemSize)
	{
		slotMaxItemSize = ItemSize.Tiny;
		foreach (UI_ItemSlot slot in InventorySlotCache.InventorySlots)
		{
			if (slot.Item != null)
			{
				var storageObj = slot.Item.GetComponent<StorageObject>();
				if (storageObj != null)
				{
					for (int i = 0; i < storageObj.storageSlots.inventorySlots.Count; i++)
					{
						if (storageObj.storageSlots.inventorySlots[i].UUID == invSlot.UUID)
						{
							slotMaxItemSize = storageObj.maxItemSize;
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	/// Checks if player received transform update after sending interact message
	/// (Anti-blinking protection)
	public static bool SendUpdateAllowed(GameObject item)
	{
		//			if ( CustomNetworkManager.Instance._isServer ) return true;
		//			var netId = item.GetComponent<NetworkIdentity>().netId;
		//			var lastReceive = item.GetComponent<NetworkTransform>().lastSyncTime;
		//			var lastSend = InputTrigger.interactCache.ContainsKey(netId) ? InputTrigger.interactCache[netId] : 0f;
		//			if ( lastReceive < lastSend )
		//			{
		//				return CanTrySendAgain(lastSend, lastReceive);
		//			}
		//			Logger.LogTraceFormat("ItemAction allowed! {2} msgcache {0} {1}", Category.UI, InputTrigger.interactCache.Count, lastSend, item.name);
		return true;
	}

	private static bool CanTrySendAgain(float lastSend, float lastReceive)
	{
		float f = Time.time - lastSend;
		float d = lastSend - lastReceive;
		bool canTrySendAgain = f >= d || f >= 1.5;
		Logger.LogTraceFormat("canTrySendAgain = {0} {1}>={2} ", Category.UI, canTrySendAgain, f, d);
		return canTrySendAgain;
	}

	public static void SetDeathVisibility(bool vis)
	{
		//			Logger.Log("I was activated!");
		foreach (Transform child in Display.hudRight.GetComponentsInChildren<Transform>(true))
		{
			if (child.gameObject.name != "OxygenSelector" && child.gameObject.name != "PlayerHealth_UI_Hud")
			{
				child.gameObject.SetActive(vis);
			}
		}

		foreach (Transform child in Display.hudBottom.GetComponentsInChildren<Transform>(true))
		{
			Transform eh = Display.hudBottom.transform.Find("Equip-Hands");
			if (child.gameObject.name != "Panel_Hud_Bottom" && !child.transform.IsChildOf(eh) && child.gameObject.name != "Equip-Hands")
			{
				child.gameObject.SetActive(vis);
			}
		}
	}

	public void ToggleTTS(bool isOn)
	{
		ttsToggle = isOn;
	}
}