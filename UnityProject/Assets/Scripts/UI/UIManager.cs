using System.Collections;
using System.Collections.Generic;
using AdminTools;
using Audio.Managers;
using Mirror;
using UI.UI_Bottom;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	private static UIManager uiManager;
	public GUI_VariableViewer VariableViewer;
	public BookshelfViewer BookshelfViewer;
	public GUI_TextInputDialog TextInputDialog;
	public ControlAction actionControl;
	[FormerlySerializedAs("dragAndDrop")] public UIDragAndDrop uiDragAndDrop;
	public ControlDisplays displayControl;
	public ControlClothing controlClothing;
	public Hands hands;
	public ControlIntent intentControl;
	public PlayerHealthUI playerHealthUI;
	public PlayerListUI playerListUIControl;
	public Text toolTip;
	public Text pingDisplay;
	[SerializeField]
	[Tooltip("Text displaying the game's version number.")]
	public Text versionDisplay;
	public GUI_Info infoWindow;
	public ControlWalkRun walkRunControl;
	public UI_StorageHandler storageHandler;
	public BuildMenu buildMenu;
	public ZoneSelector zoneSelector;
	public bool ttsToggle;
	public static GamePad GamePad => Instance.gamePad;
	public GamePad gamePad;
	public AnimationCurve strandedZoomOutCurve;
	public AdminChatButtons adminChatButtons;
	public AdminChatWindows adminChatWindows;
	public PlayerAlerts playerAlerts;
	private bool preventChatInput;

	public static bool PreventChatInput
	{
		get { return uiManager.preventChatInput; }
		set { uiManager.preventChatInput = value; }
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

	//		public static ControlChat Chat => Instance.chatControl; //Use ChatRelay.Instance.AddToChatLog instead!
	public static PlayerHealthUI PlayerHealthUI => Instance.playerHealthUI;

	public static Hands Hands => Instance.hands;

	public static ControlIntent Intent => Instance.intentControl;

	public static ControlAction Action => Instance.actionControl;

	public static UIDragAndDrop UiDragAndDrop => Instance.uiDragAndDrop;

	public static ControlWalkRun WalkRun => Instance.walkRunControl;

	public static ControlDisplays Display => Instance.displayControl;
	public static ControlClothing ControlClothing => Instance.controlClothing;

	public static PlayerListUI PlayerListUI => Instance.playerListUIControl;

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


	private float pingUpdate;

	public static string SetToolTip
	{
		set { Instance.toolTip.text = value; }
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
			if (PlayerManager.LocalPlayerScript != null)
			{
				PlayerManager.LocalPlayerScript.playerMove.CmdSetHelpIntent(currentIntent == global::Intent.Help);
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
		DetermineInitialTargetFrameRate();
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

		adminChatButtons.transform.parent.gameObject.SetActive(false);
		SetVersionDisplay = $"Work In Progress {GameData.BuildNumber}";
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
	}

	void OnSceneChange(Scene oldScene, Scene newScene)
	{
		adminChatButtons.ClearAllNotifications();
		adminChatWindows.ResetAll();
		playerAlerts.ClearLogs();
	}

	void DetermineInitialTargetFrameRate()
	{
		if(!PlayerPrefs.HasKey(PlayerPrefKeys.TargetFrameRate))
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

	private void Update()
	{
		//Read out of ping in toolTip
		pingUpdate += Time.deltaTime;
		if (pingUpdate >= 5f)
		{
			pingUpdate = 0f;
			pingDisplay.text = $"ping: {(NetworkTime.rtt * 1000):0}ms";
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
		var targetWorldPosition = PlayerManager.LocalPlayer.transform.position + offsetFromPlayer.To3Int();
		var targetTilePosition = PlayerManager.LocalPlayer.TileWorldPosition() + offsetFromPlayer;
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
			Logger.LogWarningFormat("Tried to destroy progress bar with unrecognized id {0}, nothing will be done.",
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
	public static ProgressBar _ServerStartProgress(IProgressAction progressAction, ActionTarget actionTarget,
		float timeForCompletion,
		GameObject player)
	{
		//convert to an offset so local position ends up being correct even on moving matrix
		var offsetFromPlayer = actionTarget.TargetWorldPosition.To2Int() - player.TileWorldPosition();
		//convert to local position so it appears correct on moving matrix
		//do not use tileworldposition for actual spawn position - bar will appear shifted on moving matrix
		var targetWorldPosition = player.transform.position + offsetFromPlayer.To3Int();
		var targetTilePosition = player.TileWorldPosition() + offsetFromPlayer;
		var targetMatrixInfo = MatrixManager.AtPoint(targetTilePosition.To3Int(), true);
		var targetParent = targetMatrixInfo.Objects;
		//snap to local position
		var targetLocalPosition = targetParent.transform.InverseTransformPoint(targetWorldPosition).RoundToInt();

		//back to world so we can call PoolClientInstantiate
		targetWorldPosition = targetParent.transform.TransformPoint(targetLocalPosition);
		var barObject = Spawn.ClientPrefab("ProgressBar", targetWorldPosition, targetParent).GameObject;
		var progressBar = barObject.GetComponent<ProgressBar>();

		//make sure it should start and call start hooks
		var startProgressInfo = new StartProgressInfo(timeForCompletion, actionTarget, player, progressBar);
		if (!progressAction.OnServerStartProgress(startProgressInfo))
		{
			//stop it without even having started it
			Logger.LogTraceFormat("Server cancelling progress start, OnServerStartProgress=false for {0}", Category.ProgressAction,
				startProgressInfo);
			Despawn.ClientSingle(barObject);
			return null;
		}


		progressBar._ServerStartProgress(progressAction, startProgressInfo);
		Instance.progressBars.Add(progressBar.ID, progressBar);

		Logger.LogTraceFormat("Server started progress bar {0} for {1}", Category.ProgressAction, progressBar.ID,
			startProgressInfo);

		return progressBar;
	}

	/// <summary>
	/// Links the UI slots to the spawned local player. Should only be called after local player has been spawned / set
	/// </summary>
	public static void LinkUISlots()
	{
		//link the UI slots to this player
		foreach (var uiSlot in Instance.GetComponentsInChildren<UI_ItemSlot>(true))
		{
			uiSlot.LinkToLocalPlayer();
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
}