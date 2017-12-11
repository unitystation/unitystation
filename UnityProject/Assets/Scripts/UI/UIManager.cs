using InputControl;
using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        public ControlChat chatControl;
        public Hands hands;
        public ControlIntent intentControl;
        public ControlAction actionControl;
        public ControlWalkRun walkRunControl;
        public ControlDisplays displayControl;
        public PlayerHealthUI playerHealthUI;
        public PlayerListUI playerListUIControl;
        public DisplayManager displayManager;
        public Text toolTip;
        public InventorySlotCache inventorySlotCache;

        private static UIManager uiManager;

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

        public static ControlChat Chat
        {
            get
            {
                return Instance.chatControl;
            }
        }

        public static PlayerHealthUI PlayerHealthUI
        {
            get
            {
                return Instance.playerHealthUI;
            }
        }

        public static Hands Hands
        {
            get
            {
                return Instance.hands;
            }
        }

        public static ControlIntent Intent
        {
            get
            {
                return Instance.intentControl;
            }
        }

        public static ControlAction Action
        {
            get
            {
                return Instance.actionControl;
            }
        }

        public static ControlWalkRun WalkRun
        {
            get
            {
                return Instance.walkRunControl;
            }
        }

        public static ControlDisplays Display
        {
            get
            {
                return Instance.displayControl;
            }
        }

        public static PlayerListUI PlayerListUI
        {
            get
            {
                return Instance.playerListUIControl;
            }
        }

        public static DisplayManager DisplayManager
        {
            get
            {
                return Instance.displayManager;
            }
        }

        public static string SetToolTip
        {
            set
            {
                Instance.toolTip.text = value;
            }
        }

        public static InventorySlotCache InventorySlots
        {
            get
            {
                return Instance.inventorySlotCache;
            }
        }

        public static void ResetAllUI()
        {
            UI_ItemSlot[] slots = Instance.GetComponentsInChildren<UI_ItemSlot>();
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
        /// use this for client UI mangling attepts
        /// </summary>
        public static bool TryUpdateSlot(UISlotObject slotInfo)
        {
            if (!CanPutItemToSlot(slotInfo)) return false;
            InventoryInteractMessage.Send(slotInfo.Slot, slotInfo.SlotContents, true);
            UpdateSlot(slotInfo);
            return true;
        }

        /// <summary>
        /// rather direct method that doesn't check anything.
        /// probably should check if you CanPutItemToSlot before using it
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
            if (proposedSlotInfo.IsEmpty() || !SendUpdateAllowed(proposedSlotInfo.SlotContents)) return false;
            var uiItemSlot = InventorySlots[proposedSlotInfo.Slot];
            var lps = PlayerManager.LocalPlayerScript;
            if (!lps || lps.canNotInteract() ||
                 uiItemSlot == null || uiItemSlot.IsFull ||
                 !uiItemSlot.CheckItemFit(proposedSlotInfo.SlotContents)) return false;
            return true;
        }

		public static string FindEmptySlotForItem(GameObject itemToPlace)
		{
			foreach (UI_ItemSlot slot in Instance.inventorySlotCache) {
				UISlotObject slottingAttempt = new UISlotObject(slot.eventName, itemToPlace);
				if(CanPutItemToSlot(slottingAttempt)) {
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
            var f = Time.time - lastSend;
            var d = lastSend - lastReceive;
            var canTrySendAgain = f >= d || f >= 1.5;
            Debug.LogFormat("canTrySendAgain = {0} {1}>={2} ", canTrySendAgain, f, d);
            return canTrySendAgain;
        }

        private static void ClearObjectIfNotInSlot(UISlotObject slotInfo)
        {
            for (var i = 0; i < InventorySlots.Length; i++)
            {
                if (InventorySlots[i].eventName.Equals(slotInfo.Slot) || !InventorySlots[i].Item) continue;
                if (InventorySlots[i].Item.Equals(slotInfo.SlotContents))
                {
                    InventorySlots[i].Clear();
                }
            }
        }

        /// <summary>
        /// Current Intent status
        /// </summary>
        public static Intent CurrentIntent { get; set; }

        /// <summary>
        /// What is DamageZoneSeclector currently set at
        /// </summary>
        public static BodyPartType DamageZone { get; set; }

        /// <summary>
        /// Is throw selected?
        /// </summary>
        public static bool IsThrow { get; set; }

        /// <summary>
        /// Is Oxygen On?
        /// </summary>
        public static bool IsOxygen { get; set; }
    }
}