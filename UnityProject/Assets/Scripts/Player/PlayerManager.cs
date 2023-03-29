using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Objects.Shuttles;
using UnityEngine;
using UnityEngine.SceneManagement;
using Player;
using Shared.Util;
using Systems.Character;

public class PlayerManager : MonoBehaviour
{
	private static PlayerManager playerManager;

	public static IPlayerControllable MovementControllable { get; private set; }

	public static ShuttleConsole ShuttleConsole { get;  set; } //So Hardcoded for RCS but I don't want to mess around with messages and Make a mess of new movement

	public static Equipment Equipment { get; private set; }

	/// <summary>The player GameObject. Null if not in game.</summary>
	public static GameObject LocalPlayerObject {
		get
		{
			if (LocalMindScript != null)
			{
				return LocalMindScript.GetDeepestBody().gameObject;
			}
			else if (LocalViewerScript != null)
			{
				return LocalViewerScript.gameObject;
			}

			return null;
		}
	}

	/// <summary>The player script for the player while in the game.</summary>
	public static PlayerScript LocalPlayerScript => LocalPlayerObject.OrNull()?.GetComponent<PlayerScript>(); //TODO Maybe a bit lagg

	public static Mind LocalMindScript { get; private set; }

	/// <summary>The player script for the player while in the lobby.</summary>
	public static JoinedViewer LocalViewerScript { get; private set; }

	public static CharacterManager CharacterManager { get; } = new CharacterManager();

	public static bool HasSpawned { get; private set; }

	public static CharacterSheet ActiveCharacter => CharacterManager.ActiveCharacter;

	private int mobIDcount;

	public static PlayerManager Instance => FindUtils.LazyFindObject(ref playerManager);

	private void Start()
	{
		CharacterManager.Init();
	}

	private void OnEnable()
	{
		EventManager.AddHandler(Event.PlayerDied, OnPlayerDeath);
		EventManager.AddHandler(Event.PlayerRejoined, OnRejoinPlayer);
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(Event.PlayerDied, OnPlayerDeath);
		EventManager.RemoveHandler(Event.PlayerRejoined, OnRejoinPlayer);
		PlayerPrefs.Save();
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void OnRejoinPlayer()
	{
		StartCoroutine(WaitForCamera());
	}

	IEnumerator WaitForCamera()
	{
		while (LocalPlayerObject == null
		       || Vector2.Distance(Camera2DFollow.followControl.transform.position,
			       Camera2DFollow.followControl.target.position) > 5f)
		{
			yield return WaitFor.EndOfFrame;
		}

		UIManager.Display.RejoinedEvent();
	}

	private void UpdateMe()
	{

		var move = GetMovementAction();
		if (ShuttleConsole != null)
		{
			if (move.moveActions.Length > 0)
			{
				ShuttleConsole.CmdMove(Orientation.From(GetMovementAction().ToPlayerMoveDirection().ToVector()));
				return;
			}
		}


		if (MovementControllable != null)
		{
			MovementControllable.ReceivePlayerMoveAction(move);
		}
		else
		{
			if (move.Direction().magnitude > 0 && LocalMindScript != null)
			{
				LocalMindScript.CmdSpawnPlayerGhost();
			}
		}



	}

	private void OnLevelFinishedLoading(Scene oldScene, Scene newScene)
	{
		Reset();
	}

	public static void Reset()
	{
		HasSpawned = false;
		EventManager.Broadcast(Event.DisableInternals);
	}


	public void OnDestroy()
	{
		HasSpawned = false;
	}

	public static void SetViewerForControl(JoinedViewer viewer)
	{
		LocalViewerScript = viewer;
	}

	public static void SetPlayerForControl(GameObject playerObjToControl, IPlayerControllable movementControllable)
	{
		Equipment = playerObjToControl.GetComponent<Equipment>();

		Camera2DFollow.followControl.target = playerObjToControl.transform;

		HasSpawned = true;

		SetMovementControllable(movementControllable);
	}

	public static void SetMind(Mind inMind)
	{
		LocalMindScript = inMind;
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
	public PlayerAction GetMovementAction()
	{
		// Stores the directions the player will move in.
		List<int> actionKeys = new List<int>();

		// Only move if player is out of UI
		if (!(LocalPlayerObject == gameObject && UIManager.IsInputFocus))
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
		EventManager.Broadcast(Event.DisableInternals);
	}

	public int GetMobID()
	{
		mobIDcount++;
		return mobIDcount;
	}
}
