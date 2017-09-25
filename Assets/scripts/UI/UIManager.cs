using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InputControl;
using PlayGroup;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace UI
{
	public class UIManager: MonoBehaviour
	{
		public ControlChat chatControl;
		public Hands hands;
		public ControlIntent intentControl;
		public ControlAction actionControl;
		public ControlWalkRun walkRunControl;
		public ControlDisplays displayControl;
		public PlayerHealthUI playerHealth;
		public PlayerListUI playerListUIControl;
		public Text toolTip;
		public InventorySlotCache inventorySlotCache;

		private static UIManager uiManager;

		public static UIManager Instance {
			get {
				if (!uiManager) {
					uiManager = FindObjectOfType<UIManager>();
				}

				return uiManager;
			}
		}

		public static ControlChat Chat {
			get {
				return Instance.chatControl;
			}
		}

		public static PlayerHealthUI PlayerHealth {
			get {
				return Instance.playerHealth;
			}
		}

		public static Hands Hands {
			get {
				return Instance.hands;
			}
		}

		public static ControlIntent Intent {
			get {
				return Instance.intentControl;
			}
		}

		public static ControlAction Action {
			get {
				return Instance.actionControl;
			}
		}

		public static ControlWalkRun WalkRun {
			get {
				return Instance.walkRunControl;
			}
		}

		public static ControlDisplays Display {
			get {
				return Instance.displayControl;
			}
		}

		public static PlayerListUI PlayerListUI {
			get {
				return Instance.playerListUIControl;
			}
		}

		public static string SetToolTip {
			set { 
				Instance.toolTip.text = value;
			}
		}

		public static InventorySlotCache InventorySlots {
			get {
				return Instance.inventorySlotCache;
			}
		}

		public static void ResetAllUI(){
			UI_ItemSlot[] slots = Instance.GetComponentsInChildren<UI_ItemSlot>();
			foreach (UI_ItemSlot slot in slots) {
				slot.Reset();
			}

			foreach (CritListener listener in UI.UIManager.Instance.GetComponentsInChildren<CritListener>()) {
				listener.Reset();
			}

			foreach (DamageMonitorListener listener in UI.UIManager.Instance.GetComponentsInChildren<DamageMonitorListener>()) {
				listener.Reset();
			}
		}
		
		public static void UpdateSlot(UISlotObject slotInfo)
		{
//			Debug.LogFormat("Updating slots: {0}", slotInfo);
			InputTrigger.Touch(slotInfo.SlotContents);
			InventorySlots[slotInfo.Slot].SetItem(slotInfo.SlotContents);
			ClearObjectIfNotInSlot(slotInfo);
		}

		public static bool CanPutItemToSlot(UISlotObject proposedSlotInfo)
		{
			if ( !ItemActionAllowed(proposedSlotInfo.SlotContents) ) return false;
			var uiItemSlot = InventorySlots[proposedSlotInfo.Slot];
			if ( uiItemSlot == null || uiItemSlot.IsFull /*insert more prechecks here*/) return false;
			return true;
		}
		
		/// Checks if player received transform update after sending interact message
		public static bool ItemActionAllowed(GameObject item)
		{
			if ( CustomNetworkManager.Instance._isServer ) return true;
			var netId = item.GetComponent<NetworkIdentity>().netId;
			var lastReceive = item.GetComponent<NetworkTransform>().lastSyncTime;
			var lastSend = InputTrigger.interactCache.ContainsKey(netId) ? InputTrigger.interactCache[netId] : 0f;
			if ( lastReceive < lastSend )
			{
				return CanTrySendAgain(lastSend, lastReceive);
			}
			Debug.LogFormat("ItemAction allowed! {2} msgcache {0} {1}", InputTrigger.interactCache.Count, lastSend, item.name);
			return true;
		}

		private static bool CanTrySendAgain(float lastSend, float lastReceive)
		{
			var f = Time.time - lastSend;
			var d = lastSend - lastReceive;
			var canTrySendAgain = f >= d || f >= 1;
			Debug.LogFormat("canTrySendAgain = {0} {1}>={2} ",canTrySendAgain, f, d);
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