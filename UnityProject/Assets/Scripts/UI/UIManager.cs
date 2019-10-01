using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	private static UIManager uiManager;
	public GUI_VariableViewer VariableViewer;
	public BookshelfViewer BookshelfViewer;
	public ControlAction actionControl;
	public DragAndDrop dragAndDrop;
	public ControlDisplays displayControl;
	public DisplayManager displayManager;
	public Hands hands;
	public ControlIntent intentControl;
	public InventorySlotCache inventorySlotCache;
	public PlayerHealthUI playerHealthUI;
	public PlayerListUI playerListUIControl;
	public AlertUI alertUI;
	public Text toolTip;
	public Text pingDisplay;
	public ControlWalkRun walkRunControl;
	public UI_StorageHandler storageHandler;
	public ZoneSelector zoneSelector;
	public bool ttsToggle;
	public static GamePad GamePad => Instance.gamePad;
	public GamePad gamePad;
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

	///Global flag for when we are using the mouse to do something that shouldn't cause interaction with the game.
	public static bool IsMouseInteractionDisabled
	{
		get
		{
			return Instance && Instance.isMouseInteractionDisabled;
		}
		set
		{
			if (!Instance)
			{
				return;
			}
			Instance.isMouseInteractionDisabled = value;
		}
	}

	private bool isMouseInteractionDisabled;

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

#if UNITY_ANDROID || UNITY_IOS //|| UNITY_EDITOR
	public static bool UseGamePad = true;
#else
	public static bool UseGamePad = false;
#endif

	//		public static ControlChat Chat => Instance.chatControl; //Use ChatRelay.Instance.AddToChatLog instead!
	public static ProgressBar ProgressBar => Instance.progressBar;
	public static PlayerHealthUI PlayerHealthUI => Instance.playerHealthUI;
	public static AlertUI AlertUI => Instance.alertUI;

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

	public static string SetPingDisplay
	{
		set { Instance.pingDisplay.text = value; }
	}

	public static InventorySlotCache InventorySlots => Instance.inventorySlotCache;

	/// <summary>
	///     Current Intent status
	/// </summary>
	public static Intent CurrentIntent
	{
		get => currentIntent;
		set
		{
			currentIntent = value;
			//update the intent of the player so it can be synced
			if (PlayerManager.LocalPlayerScript != null)
			{
				PlayerManager.LocalPlayerScript.playerMove.IsHelpIntent = value == global::Intent.Help;
			}
		}
	}

	private static Intent currentIntent;

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

	private void Start()
	{
		Logger.Log("Touchscreen support = " + CommonInput.IsTouchscreen, Category.UI);

		if (!PlayerPrefs.HasKey(PlayerPrefKeys.TTSToggleKey))
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.TTSToggleKey, 0);
			ttsToggle = false;
			PlayerPrefs.Save();
		}
		else
		{
			ttsToggle = PlayerPrefs.GetInt(PlayerPrefKeys.TTSToggleKey) == 1;
		}
	}

	public static void ToggleTTS(bool activeState)
	{
		Instance.ttsToggle = activeState;
		PlayerPrefs.SetInt(PlayerPrefKeys.TTSToggleKey, activeState ? 1 : 0);
		PlayerPrefs.Save();
	}

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
		IsOxygen = false;
		GamePad.gameObject.SetActive(UseGamePad);
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

	public static bool CanPutItemToSlot(InventorySlot inventorySlot, GameObject item, PlayerScript playerScript)
	{
		if (item == null || inventorySlot.Item != null)
		{
			return false;
		}
		if (playerScript.canNotInteract())
		{
			return false;
		}
		var uiItemSlot = InventorySlotCache.GetSlotByEvent(inventorySlot.equipSlot);
		if (!uiItemSlot.CheckItemFit(item))
		{
			return false;
		}
		return true;
	}
}