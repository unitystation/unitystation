using UnityEngine;
using System.Collections;
using PlayGroup;
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
		public DisplayManager displayManager;
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

		public static DisplayManager DisplayManager {
			get {
				return Instance.displayManager;
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
			InventorySlots[slotInfo.Slot].SetItem(slotInfo.SlotContents);
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