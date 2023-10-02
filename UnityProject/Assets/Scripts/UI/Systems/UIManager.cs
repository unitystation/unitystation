using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Mirror;
using AdminTools;
using AdminTools.VariableViewer;
using Audio.Managers;
using Initialisation;
using Learning;
using Logs;
using UI;
using UI.Core;
using UI.Core.Windows;
using UI.Chat_UI;
using UI.Jobs;
using UI.UI_Bottom;
using UI.Windows;
using Systems.CraftingV2.GUI;
using Systems.Faith.UI;
using UI.Character;
using UI.Systems.AdminTools.DevTools;
using UI.Systems.EndRound;
using UI.Systems.ServerInfoPanel;
using UI.Systems.Tooltips.HoverTooltips;

public class UIManager : MonoBehaviour, IInitialise
{
	private static UIManager uiManager;
	public GUI_VariableViewer VariableViewer;
	public UI_BooksInBookshelf UI_BooksInBookshelf;
	public LibraryUI LibraryUI;
	public GUI_TextInputDialog TextInputDialog;
	public ControlAction actionControl;
	[FormerlySerializedAs("dragAndDrop")] public UIDragAndDrop uiDragAndDrop;
	public ControlDisplays displayControl;
	public ControlClothing controlClothing;
	public PanelHudBottomController panelHudBottomController;
	public ControlInternals internalControls;
	public PlayerExaminationWindowUI playerExaminationWindow;
	public ControlIntent intentControl;
	public PlayerHealthUI playerHealthUI;
	public StatsTab statsTab;
	public Text toolTip;
	public Text pingDisplay;

	public ClientAlertManager ClientAlertManager;

	[SerializeField] [Tooltip("Text displaying the game's version number.")]
	public Text versionDisplay;

	public GUI_Info infoWindow;
	public TeleportWindow teleportWindow;
	[SerializeField] private GhostRoleWindow ghostRoleWindow = default;
	public UI_StorageHandler storageHandler;
	public BuildMenu buildMenu;
	public ZoneSelector zoneSelector;
	public bool ttsToggle;
	public static GamePad GamePad => Instance.gamePad;
	public GamePad gamePad;
	public AnimationCurve strandedZoomOutCurve;
	public AdminChatButtons adminChatButtons;
	public AdminChatButtons mentorChatButtons;
	public AdminChatButtons prayerChatButtons;
	public AdminChatWindows adminChatWindows;
	public ProfileScrollView profileScrollView;
	public PlayerAlerts playerAlerts;
	[FormerlySerializedAs("antagBanner")] public GUIAntagBanner spawnBanner;
	private static bool preventChatInput;
	[SerializeField] [Range(0.1f, 10f)] private float PhoneZoomFactor = 1.6f;
	public LobbyUIPlayerListController lobbyUIPlayerListController = null;

	public SurgeryDialogue SurgeryDialogue;

	public CrayonUI CrayonUI;

	public UI_SlotManager UI_SlotManager;

	public GeneralInputField GeneralInputField;

	public CraftingMenu CraftingMenu;

	public SplittingMenu SplittingMenu;

	public GUI_DevTileChanger TileChanger;

	public CharacterSettings CharacterSettings;

	[field: SerializeField] public ServerInfoPanelWindow ServerInfoPanelWindow { get; private set; }

	public RoundEndScoreScreen ScoreScreen;

	[field: SerializeField] public ExpLevelUI FirstTimePlayerExperienceScreen { get; set; }
	[field: SerializeField] public ColorPicker GlobalColorPicker { get; set; }

	public static bool PreventChatInput
	{
		get { return preventChatInput; }
		set { preventChatInput = value; }
	}

	//map from progress bar id to actual progress bar component.
	private Dictionary<int, ProgressBar> progressBars = new Dictionary<int, ProgressBar>();

	/// <summary>
	/// Current progress bars
	/// </summary>
	public IEnumerable<ProgressBar> ProgressBars => progressBars.Values;

	///Global flag for focused input field. Movement keystrokes are ignored if true.
	/// <see cref="InputFieldFocus"/> handles this flag automatically
	public static bool IsInputFocus
	{
		get { return Instance && Instance.isInputFocus; }
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
		get { return Instance && Instance.isMouseInteractionDisabled; }
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

	public static bool IsTablet => DeviceDiagonalSizeInInches > 6.5f && AspectRatio < 2f;

	public static float AspectRatio =>
		(float)Mathf.Max(Screen.width, Screen.height) / Mathf.Min(Screen.width, Screen.height);

	public static float DeviceDiagonalSizeInInches
	{
		get
		{
			float screenWidth = Screen.width / Screen.dpi;
			float screenHeight = Screen.height / Screen.dpi;
			float diagonalInches = Mathf.Sqrt(Mathf.Pow(screenWidth, 2) + Mathf.Pow(screenHeight, 2));

			Loggy.Log("Getting mobile device screen size in inches: " + diagonalInches, Category.UI);

			return diagonalInches;
		}
	}

	//		public static ControlChat Chat => Instance.chatControl; //Use ChatRelay.Instance.AddToChatLog instead!
	public static PlayerHealthUI PlayerHealthUI => Instance.playerHealthUI;

	public static PlayerExaminationWindowUI PlayerExaminationWindow => Instance.playerExaminationWindow;

	public static ControlIntent Intent => Instance.intentControl;

	public static ControlInternals Internals => Instance.internalControls;

	public static ControlAction Action => Instance.actionControl;

	public static UIDragAndDrop UiDragAndDrop => Instance.uiDragAndDrop;

	public static ControlDisplays Display => Instance.displayControl;
	public static ControlClothing ControlClothing => Instance.controlClothing;

	public static StatsTab StatsTab => Instance.statsTab;

	public static UI_StorageHandler StorageHandler
	{
		get
		{
			if (Instance == null)
			{
				return null;
			}

			return Instance.storageHandler;
		}
	}

	public static BuildMenu BuildMenu => Instance.buildMenu;

	public static ZoneSelector ZoneSelector => Instance.zoneSelector;

	public static GUI_Info InfoWindow => Instance.infoWindow;

	public static TeleportWindow TeleportWindow => Instance.teleportWindow;
	public static GhostRoleWindow GhostRoleWindow => Instance.ghostRoleWindow;

	private float pingUpdate;

	[SerializeField] private PanelTooltipManager panelTooltipManager;
	public PanelTooltipManager PanelTooltipManager => panelTooltipManager;

	[SerializeField] private HoverTooltipUI hoverTooltipUI;
	public HoverTooltipUI HoverTooltipUI => hoverTooltipUI;

	[SerializeField] public CanvasScaler Scaler;

	[field: SerializeField] public ChaplainFirstTimeSelectScreen ChaplainFirstTimeSelectScreen { get; private set; }

	public static string SetToolTip
	{
		set
		{
			if (Instance.PanelTooltipManager == null) return;
			Instance.PanelTooltipManager.UpdateActiveTooltip(value);
		}
	}

	public static GameObject SetHoverToolTip
	{
		set
		{
			if (Instance.HoverTooltipUI == null) return;
			Instance.hoverTooltipUI.SetupTooltip(value);
		}
	}

	public static string SetVersionDisplay
	{
		set { Instance.versionDisplay.text = value; }
	}

	/// <summary>
	///     Current Intent status
	/// </summary>
	public static Intent CurrentIntent
	{
		get => currentIntent;
		set
		{
			currentIntent = value;

			//update the intent of the player on server so server knows we are swappable or not
			if (PlayerManager.LocalPlayerScript != null && PlayerManager.LocalPlayerScript.IsNormal)
			{
				PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdSetCurrentIntent(currentIntent);
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

	public InitialisationSystems Subsystem => InitialisationSystems.UIManager;

	void IInitialise.Initialise()
	{
		DetermineInitialTargetFrameRate();
		Loggy.Log("Touchscreen support = " + CommonInput.IsTouchscreen, Category.Sprites);
		InitMobile();

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

		adminChatButtons.transform.parent.gameObject.SetActive(false);
		mentorChatButtons.transform.parent.gameObject.SetActive(false);
		prayerChatButtons.transform.parent.gameObject.SetActive(false);
		SetVersionDisplay = $"Work In Progress {GameData.BuildNumber}";
	}

	private void InitMobile()
	{
		if (!Application.isMobilePlatform)
		{
			return;
		}

		if (!IsTablet) //tablets should be fine as is
		{
			Loggy.Log("Looks like it's a phone, scaling UI", Category.UI);
			var canvasScaler = GetComponent<CanvasScaler>();
			if (!canvasScaler)
			{
				return;
			}

			canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			canvasScaler.matchWidthOrHeight = 0f; //match width
			canvasScaler.referenceResolution =
				new Vector2(Screen.width / PhoneZoomFactor, canvasScaler.referenceResolution.y);

		}
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	void OnSceneChange(Scene oldScene, Scene newScene)
	{
		adminChatButtons.ClearAllNotifications();
		mentorChatButtons.ClearAllNotifications();
		prayerChatButtons.ClearAllNotifications();
		adminChatWindows.ResetAll();
		playerAlerts.ClearLogs();
	}

	void DetermineInitialTargetFrameRate()
	{
		if (!PlayerPrefs.HasKey(PlayerPrefKeys.TargetFrameRate))
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.TargetFrameRate, 99);
			PlayerPrefs.Save();
		}

		var targetFrameRate = PlayerPrefs.GetInt(PlayerPrefKeys.TargetFrameRate);
		targetFrameRate = Mathf.Clamp(targetFrameRate, 30, 144);
		PlayerPrefs.SetInt(PlayerPrefKeys.TargetFrameRate, targetFrameRate);
		PlayerPrefs.Save();

		Application.targetFrameRate = targetFrameRate;
	}

	private void UpdateMe()
	{
		//Read out of ping in toolTip
		pingUpdate += Time.deltaTime;
		if (pingUpdate >= 5f)
		{
			pingUpdate = 0f;
			pingDisplay.text = $"ping: {(NetworkTime.rtt * 1000):0}ms";
		}
	}

	public static void UpdateKeybindText(KeyAction keyAction, KeybindManager.KeyCombo keyCombo)
	{
		return;
		//TODO needs to be re-implemented with dynamic UI issue #7948

		// switch (keyAction)
		// {
		// 	case KeyAction.OpenBackpack:
		// 		Instance.panelHudBottomController.SetBackPackKeybindText(
		// 			FormatKeybind(keyCombo.MainKey)
		// 		);
		// 		break;
		// 	case KeyAction.OpenPDA:
		// 		Instance.panelHudBottomController.SetPDAKeybindText(
		// 			FormatKeybind(keyCombo.MainKey)
		// 		);
		// 		break;
		// 	case KeyAction.OpenBelt:
		// 		Instance.panelHudBottomController.SetBeltKeybindText(
		// 			FormatKeybind(keyCombo.MainKey)
		// 		);
		// 		break;
		// 	case KeyAction.PocketOne:
		// 		Instance.panelHudBottomController.SetPocketOneKeybindText(
		// 			FormatKeybind(keyCombo.MainKey)
		// 		);
		// 		break;
		// 	case KeyAction.PocketTwo:
		// 		Instance.panelHudBottomController.SetPocketTwoKeybindText(
		// 			FormatKeybind(keyCombo.MainKey)
		// 		);
		// 		break;
		// 	case KeyAction.PocketThree:
		// 		Instance.panelHudBottomController.SetPocketThreeKeybindText(
		// 			FormatKeybind(keyCombo.MainKey)
		// 		);
		// 		break;
		// 	default:
		// 		Logger.LogWarning($"There is no keybind text for KeyAction {keyAction}", Category.Keybindings);
		// 		break;
		// }
	}

	private static string FormatKeybind(KeyCode key)
	{
		string result = key.ToString();
		if (result.StartsWith("Alpha"))
			return result[result.Length - 1].ToString();

		return key.ToString();
	}

	public static void ToggleTTS(bool activeState)
	{
		Instance.ttsToggle = activeState;
		PlayerPrefs.SetInt(PlayerPrefKeys.TTSToggleKey, activeState ? 1 : 0);
		PlayerPrefs.Save();
	}

	public static void ResetAllUI()
	{
		StorageHandler.CloseStorageUI();
		Camera2DFollow.followControl.ZeroStars();
		IsOxygen = false;
		GamePad.gameObject.SetActive(UseGamePad);
	}

	/// <summary>
	/// Gets the progress bar with the specified unique id. Creates it at the specified position if it doesn't
	/// exist yet
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public static ProgressBar GetProgressBar(int id)
	{
		if (Instance.progressBars.ContainsKey(id))
		{
			return Instance.progressBars[id];
		}

		return null;
	}

	/// <summary>
	/// Creates a new progress bar for local player with the specified id and specified offset from player
	/// (parented to the matrix at the position being targeted so it moves with it)
	/// </summary>
	/// <param name="offsetFromPlayer">offset position from local player</param>
	/// <param name="progressBarId">id to assign to the new progress bar</param>
	/// <returns> the new bar</returns>
	public static ProgressBar CreateProgressBar(Vector2Int offsetFromPlayer, int progressBarId)
	{
		//convert to local position so it appears correct on moving matrix
		//do not use tileworldposition for actual spawn position - bar will appear shifted on moving matrix
		var targetWorldPosition = PlayerManager.LocalPlayerObject.transform.position + offsetFromPlayer.To3Int();
		var targetTilePosition = PlayerManager.LocalPlayerObject.TileWorldPosition() + offsetFromPlayer;
		var targetMatrixInfo = MatrixManager.AtPoint(targetTilePosition.To3Int(), true);
		var targetParent = targetMatrixInfo.Objects;
		//snap to local position
		var targetLocalPosition = targetParent.transform.InverseTransformPoint(targetWorldPosition).RoundToInt();
		//back to world so we can call PoolClientInstantiate
		targetWorldPosition = targetParent.transform.TransformPoint(targetLocalPosition);
		var barObject = Spawn.ClientPrefab("ProgressBar", targetWorldPosition, targetParent).GameObject;
		var progressBar = barObject.GetComponent<ProgressBar>();

		progressBar.ClientStartProgress(progressBarId);

		Instance.progressBars.Add(progressBarId, progressBar);

		return progressBar;
	}

	/// <summary>
	/// Destroys the progress bar.
	/// </summary>
	/// <param name="progressBarId"></param>
	public static void DestroyProgressBar(int progressBarId)
	{
		var bar = GetProgressBar(progressBarId);
		if (bar == null)
		{
			Loggy.LogWarningFormat("Tried to destroy progress bar with unrecognized id {0}, nothing will be done.",
				Category.UI, progressBarId);
		}
		else
		{
			Instance.progressBars.Remove(progressBarId);
			//note: not using poolmanager since this has no object behavior
			Despawn.ClientSingle(bar.gameObject);
		}
	}

	/// <summary>
	/// For progress action system internal use only. Please use ProgressAction.ServerStartProgress to initiate a progress action
	/// on the server side.
	/// Tries to create and begin animating a progress bar for a specific player. Returns null
	/// if progress did not begin for some reason.
	/// </summary>
	/// <param name="progressAction">progress action being performed</param>
	/// <param name="actionTarget">target of the progress action</param>
	/// <param name="timeForCompletion">how long in seconds the action should take</param>
	/// <param name="player">player performing the action</param>
	/// <returns>progress bar associated with this action (can use this to interrupt progress). Null if
	/// progress was not started for some reason (such as already in progress for this action on the specified tile).</returns>
	public static ProgressBar _ServerStartProgress(
		IProgressAction progressAction, ActionTarget actionTarget, float timeForCompletion, GameObject player)
	{
		var targetMatrixInfo = MatrixManager.AtPoint(actionTarget.TargetWorldPosition.CutToInt(), true);
		var targetParent = targetMatrixInfo.Objects;

		var barObject = Spawn.ClientPrefab("ProgressBar", actionTarget.TargetWorldPosition, targetParent).GameObject;
		var progressBar = barObject.GetComponent<ProgressBar>();

		//make sure it should start and call start hooks
		var startProgressInfo = new StartProgressInfo(timeForCompletion, actionTarget, player, progressBar);
		if (!progressAction.OnServerStartProgress(startProgressInfo))
		{
			//stop it without even having started it
			Loggy.LogTraceFormat("Server cancelling progress start, OnServerStartProgress=false for {0}",
				Category.ProgressAction,
				startProgressInfo);
			Despawn.ClientSingle(barObject);
			return null;
		}


		progressBar._ServerStartProgress(progressAction, startProgressInfo);
		Instance.progressBars.Add(progressBar.ID, progressBar);

		Loggy.LogTraceFormat("Server started progress bar {0} for {1}", Category.ProgressAction, progressBar.ID,
			startProgressInfo);

		return progressBar;
	}

	/// <summary>
	/// Links the UI slots to the spawned local player. Should only be called after local player has been spawned / set
	/// </summary>
	public static void LinkUISlots(ItemStorageLinkOrigin itemStorageLinkOrigin)
	{
		// link the UI slots to this player
		foreach (var uiSlot in Instance.GetComponentsInChildren<UI_ItemSlot>(true))
		{
			if (uiSlot.ItemStorageLinkOrigin == itemStorageLinkOrigin)
			{
				uiSlot.LinkToLocalPlayer();
			}
		}
	}

	private float originalZoom = 5f;

	public void PlayStrandedAnimation()
	{
		//turning off all the UI except for the right panel
		UIManager.PlayerHealthUI.gameObject.SetActive(false);
		UIManager.Display.hudBottomHuman.gameObject.SetActive(false);
		UIManager.Display.hudBottomGhost.gameObject.SetActive(false);
		ChatUI.Instance.CloseChatWindow();

		//play the ending video and wait for it
		Display.PlayStrandedVideo();
		StartCoroutine(WaitForStrandedVideoEnd());

		//NOTE: Disabl;ing zoom out as it's just a dumb gimmick
		//and it is causing perf issues and crashing for some people
		//start zooming out
		//StartCoroutine(StrandedZoomOut());
	}

	private IEnumerator StrandedZoomOut()
	{
		var camera = Camera.main;
		float time = 0f;
		float end = strandedZoomOutCurve[strandedZoomOutCurve.length - 1].time;
		originalZoom = camera.orthographicSize;

		while (time < end)
		{
			var curVal = strandedZoomOutCurve.Evaluate(time);
			camera.orthographicSize = curVal;
			time += Time.deltaTime;

			yield return null;
		}

		SoundAmbientManager.StopAllAudio();

	}

	private IEnumerator WaitForStrandedVideoEnd()
	{
		yield return WaitFor.Seconds(11f);
		//so we don't freak out the lighting system
		Camera.main.orthographicSize = originalZoom;
		//turn everything back on
		yield return null;
		UIManager.PlayerHealthUI.gameObject.SetActive(true);
		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			UIManager.Display.hudBottomGhost.gameObject.SetActive(true);
		}
		else
		{
			UIManager.Display.hudBottomHuman.gameObject.SetActive(true);
		}

		ChatUI.Instance.OpenChatWindow();
	}

	public void ToggleUiVisibility()
	{
		gameObject.SetActive(!gameObject.activeInHierarchy);
		ChatUI.Instance.CloseChatWindow(true);
	}
}