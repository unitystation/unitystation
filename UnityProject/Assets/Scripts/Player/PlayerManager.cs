using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
	private static PlayerManager playerManager;

	public static IPlayerControllable MovementControllable { get; private set; }
	public static GameObject LocalPlayer { get; private set; }

	public static Equipment Equipment { get; private set; }

	public static PlayerScript LocalPlayerScript { get; private set; }
	public static JoinedViewer LocalViewerScript { get; private set; }

	//For access via other parts of the game
	public static PlayerScript PlayerScript { get; private set; }

	public static bool HasSpawned { get; private set; }

	public static CharacterSettings CurrentCharacterSettings { get; set; }

	private int mobIDcount;

	public static PlayerManager Instance
	{
		get
		{
			if (!playerManager)
			{
				playerManager = FindObjectOfType<PlayerManager>();
			}

			return playerManager;
		}
	}

#if UNITY_EDITOR	//Opening the station scene instead of going through the lobby
	private void Awake()
	{
		if (CurrentCharacterSettings != null)
		{
			return;
		}
		// Load CharacterSettings from PlayerPrefs or create a new one
		string unescapedJson = Regex.Unescape(PlayerPrefs.GetString("currentcharacter"));
		var deserialized = JsonConvert.DeserializeObject<CharacterSettings>(unescapedJson);
		PlayerCustomisationDataSOs.Instance.ValidateCharacterSettings(ref deserialized);
		CurrentCharacterSettings = deserialized ?? new CharacterSettings();
	}
#endif

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnLevelFinishedLoading;
		EventManager.AddHandler(EVENT.PlayerDied, OnPlayerDeath);
		EventManager.AddHandler(EVENT.PlayerRejoined, OnRejoinPlayer);
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnLevelFinishedLoading;
		EventManager.RemoveHandler(EVENT.PlayerDied, OnPlayerDeath);
		EventManager.RemoveHandler(EVENT.PlayerRejoined, OnRejoinPlayer);
		PlayerPrefs.Save();
	}

	private void OnRejoinPlayer()
	{
		StartCoroutine(WaitForCamera());
	}

	IEnumerator WaitForCamera()
	{
		while (LocalPlayer == null
		       || Vector2.Distance(Camera2DFollow.followControl.transform.position,
			       Camera2DFollow.followControl.target.position) > 5f)
		{
			yield return WaitFor.EndOfFrame;
		}

		UIManager.Display.RejoinedEvent();
	}

	private void Update()
	{
		if (MovementControllable != null)
		{
			MovementControllable.RecievePlayerMoveAction(GetMovementActions());
		}
	}

	private void OnLevelFinishedLoading(Scene oldScene, Scene newScene)
	{
		Reset();
	}

	public static void Reset()
	{
		HasSpawned = false;
		EventManager.Broadcast(EVENT.DisableInternals);
	}

	public static void SetViewerForControl(JoinedViewer viewer)
	{
		LocalViewerScript = viewer;
	}

	public static void SetPlayerForControl(GameObject playerObjToControl, IPlayerControllable movementControllable)
	{
		LocalPlayer = playerObjToControl;
		LocalPlayerScript = playerObjToControl.GetComponent<PlayerScript>();
		Equipment = playerObjToControl.GetComponent<Equipment>();

		PlayerScript =
			LocalPlayerScript; // Set this on the manager so it can be accessed by other components/managers
		Camera2DFollow.followControl.target = LocalPlayer.transform;

		HasSpawned = true;

		SetMovementControllable(movementControllable);
	}

	/// <summary>
	/// Set the object that is going to be controlled by the movement keys
	/// You can use this to pass controls over to a vehicle, camera or anything really
	/// </summary>
	public static void SetMovementControllable(IPlayerControllable controllable)
	{
		MovementControllable = controllable;
	}

	/// <summary>
	/// Processes currenlty held directional movement keys into a PlayerAction.
	/// Opposite moves on the X or Y axis cancel out, not moving the player in that axis.
	/// Moving while dead spawns the player's ghost.
	/// </summary>
	/// <returns> A PlayerAction containing up to two (non-opposite) movement directions.</returns>
	public PlayerAction GetMovementActions()
	{
		// Stores the directions the player will move in.
		List<int> actionKeys = new List<int>();

		// Only move if player is out of UI
		if (!(LocalPlayer == gameObject && UIManager.IsInputFocus))
		{
			bool moveL = KeyboardInputManager.CheckMoveAction(MoveAction.MoveLeft);
			bool moveR = KeyboardInputManager.CheckMoveAction(MoveAction.MoveRight);
			bool moveU = KeyboardInputManager.CheckMoveAction(MoveAction.MoveDown);
			bool moveD = KeyboardInputManager.CheckMoveAction(MoveAction.MoveUp);
			// Determine movement on each axis (cancelling opposite moves)
			int moveX = (moveR ? 1 : 0) - (moveL ? 1 : 0);
			int moveY = (moveD ? 1 : 0) - (moveU ? 1 : 0);

			if (moveX != 0 || moveY != 0)
			{

					switch (moveX)
					{
						case 1:
							actionKeys.Add((int)MoveAction.MoveRight);
							break;
						case -1:
							actionKeys.Add((int)MoveAction.MoveLeft);
							break;
						default:
							break; // Left, Right cancelled or not pressed
					}
					switch (moveY)
					{
						case 1:
							actionKeys.Add((int)MoveAction.MoveUp);
							break;
						case -1:
							actionKeys.Add((int)MoveAction.MoveDown);
							break;
						default:
							break; // Up, Down cancelled or not pressed
					}
			}
		}

		return new PlayerAction { moveActions = actionKeys.ToArray() };
	}

	private void OnPlayerDeath()
	{
		EventManager.Broadcast(EVENT.DisableInternals);
	}

	public int GetMobID()
	{
		mobIDcount++;
		return mobIDcount;
	}
}