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
		public PlayerHealth playerHealth;
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

		public static PlayerHealth PlayerHealth {
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

		/// <summary>
		/// Current Intent status
		/// </summary>
		public static Intent CurrentIntent { get; set; }

		/// <summary>
		/// What is DamageZoneSeclector currently set at
		/// </summary>
		public static DamageZoneSelector DamageZone { get; set; }

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