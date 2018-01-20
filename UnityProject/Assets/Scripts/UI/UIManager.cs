using PlayGroup;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIManager : MonoBehaviour
	{
		private static UIManager uiManager;
		public ControlAction actionControl;
		public ControlChat chatControl;
		public ControlDisplays displayControl;
		public DisplayManager displayManager;
		public Hands hands;
		public ControlIntent intentControl;
		public InventorySlotCache inventorySlotCache;
		public PlayerHealthUI playerHealthUI;
		public PlayerListUI playerListUIControl;
		public Text toolTip;
		public ControlWalkRun walkRunControl;

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

		public static ControlChat Chat => Instance.chatControl;

		public static PlayerHealthUI PlayerHealthUI => Instance.playerHealthUI;

		public static Hands Hands => Instance.hands;

		public static ControlIntent Intent => Instance.intentControl;

		public static ControlAction Action => Instance.actionControl;

		public static ControlWalkRun WalkRun => Instance.walkRunControl;

		public static ControlDisplays Display => Instance.displayControl;

		public static PlayerListUI PlayerListUI => Instance.playerListUIControl;

		public static DisplayManager DisplayManager => Instance.displayManager;

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
			InventoryInteractMessage.Send(slotInfo.Slot, slotInfo.SlotContents, true, Vector3.zero);
			UpdateSlot(slotInfo);
			return true;
		}

		/// <summary>
		///     rather direct method that doesn't check anything.
		///     probably should check if you CanPutItemToSlot before using it
		/// </summary>
		public static void UpdateSlot(UISlotObject slotInfo)
		{
			//			Debug.LogFormat("Updating slots: {0}", slotInfo);
			//			InputTrigger.Touch(slotInfo.SlotContents);
			InventorySlots[slotInfo.Slot].SetItem(slotInfo.SlotContents);
			ClearObjectIfNotInSlot(slotInfo);
		}

		public static bool CanPutItemToSlot(UISlotObject proposedSlotInfo)
		{
			if (proposedSlotInfo.IsEmpty() || !SendUpdateAllowed(proposedSlotInfo.SlotContents))
			{
				return false;
			}
			UI_ItemSlot uiItemSlot = InventorySlots[proposedSlotInfo.Slot];
			PlayerScript lps = PlayerManager.LocalPlayerScript;
			if (!lps || lps.canNotInteract() ||
			    uiItemSlot == null || uiItemSlot.IsFull ||
			    !uiItemSlot.CheckItemFit(proposedSlotInfo.SlotContents))
			{
				return false;
			}
			return true;
		}

		public static string FindEmptySlotForItem(GameObject itemToPlace)
		{
			foreach (UI_ItemSlot slot in Instance.inventorySlotCache)
			{
				UISlotObject slottingAttempt = new UISlotObject(slot.eventName, itemToPlace);
				if (CanPutItemToSlot(slottingAttempt))
				{
					return slot.eventName;
				}
			}

			return null;
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
			//			Debug.LogFormat("ItemAction allowed! {2} msgcache {0} {1}", InputTrigger.interactCache.Count, lastSend, item.name);
			return true;
		}

		private static bool CanTrySendAgain(float lastSend, float lastReceive)
		{
			float f = Time.time - lastSend;
			float d = lastSend - lastReceive;
			bool canTrySendAgain = f >= d || f >= 1.5;
			Debug.LogFormat("canTrySendAgain = {0} {1}>={2} ", canTrySendAgain, f, d);
			return canTrySendAgain;
		}

		private static void ClearObjectIfNotInSlot(UISlotObject slotInfo)
		{
			for (int i = 0; i < InventorySlots.Length; i++)
			{
				if (InventorySlots[i].eventName.Equals(slotInfo.Slot) || !InventorySlots[i].Item)
				{
					continue;
				}
				if (InventorySlots[i].Item.Equals(slotInfo.SlotContents))
				{
					InventorySlots[i].Clear();
				}
			}
		}

		public static void SetDeathVisibility(bool vis)
		{
			Debug.Log("I was activated!");
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
	}
}