using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

/// <summary>
///     Player move queues the directional move keys
///     to be processed along with the server.
///     It also changes the sprite direction and
///     handles interaction with objects that can
///     be walked into it.
/// </summary>
public class PlayerMove : NetworkBehaviour
{
	private PlayerScript playerScript;
	public PlayerScript PlayerScript => playerScript ? playerScript : ( playerScript = GetComponent<PlayerScript>() );

	public bool diagonalMovement;

	[SyncVar] public bool allowInput = true;
	[SyncVar(hook=nameof(OnRestrainedChanged))] private bool restrained = false;
	//callback invoked when we are unbuckled.
	private Action onUnbuckled;

	/// <summary>
	/// Tracks the server's idea of whether we have help intent
	/// </summary>
	[SyncVar] private bool serverIsHelpIntent = true;
	/// <summary>
	/// Tracks our idea of whether we have help intent so we can use it for client prediction
	/// </summary>
	private bool localIsHelpIntent = true;

	/// <summary>
	/// True iff this player is set to help intent, thus should swap places with players
	/// that they collide with if the other player also has help intent
	/// </summary>
	public bool IsHelpIntent
	{
		get
		{
			if (isLocalPlayer)
			{
				return localIsHelpIntent;
			}
			else
			{
				return serverIsHelpIntent;
			}
		}
		set
		{
			if (isLocalPlayer)
			{
				localIsHelpIntent = value;
				//tell the server we want this to be our setting
				CmdChangeHelpIntent(value);
			}
			else
			{
				//accept what the server is telling us about someone other than our local player
				serverIsHelpIntent = value;
			}
		}
	}

	private readonly List<MoveAction> moveActionList = new List<MoveAction>();

	public MoveAction[] moveList =
	{
		MoveAction.MoveUp, MoveAction.MoveLeft, MoveAction.MoveDown, MoveAction.MoveRight
	};

	private UserControlledSprites playerSprites;

	[HideInInspector] public PlayerNetworkActions pna;

	[FormerlySerializedAs( "speed" )]
	public float RunSpeed = 6;
	public float WalkSpeed = 3;
	public float CrawlSpeed = 0.8f;
	/// <summary>
	/// Player will fall when pushed with such speed
	/// </summary>
	public float PushFallSpeed = 10;

	private RegisterPlayer registerPlayer;
	private Matrix matrix => registerPlayer.Matrix;

	/// temp solution for use with the UI network prediction
	public bool isMoving { get; } = false;

	/// <summary>
	/// Whether character is restrained, such as by being buckled to a chair
	/// </summary>
	public bool IsRestrained => restrained;

	private void Start()
	{
		playerSprites = gameObject.GetComponent<UserControlledSprites>();

		registerPlayer = GetComponent<RegisterPlayer>();
		pna = gameObject.GetComponent<PlayerNetworkActions>();
	}

	[Command]
	private void CmdChangeHelpIntent(bool isHelpIntent)
	{
		serverIsHelpIntent = isHelpIntent;
	}

	public PlayerAction SendAction()
	{
		List<int> actionKeys = new List<int>();

		for (int i = 0; i < moveList.Length; i++)
		{
			if (PlayerManager.LocalPlayer == gameObject && UIManager.IsInputFocus)
			{
				return new PlayerAction { moveActions = actionKeys.ToArray() };
			}

			// if (CommonInput.GetKey(moveList[i]) && allowInput)
			// {
			// 	actionKeys.Add((int)moveList[i]);
			// }
			if (KeyboardInputManager.CheckMoveAction(moveList[i]) && allowInput && !restrained)
			{
				actionKeys.Add((int)moveList[i]);
			}
		}

		return new PlayerAction { moveActions = actionKeys.ToArray() };
	}

	public Vector3Int GetNextPosition(Vector3Int currentPosition, PlayerAction action, bool isReplay, Matrix curMatrix = null)
	{
		if (!curMatrix)
		{
			curMatrix = matrix;
		}

		Vector3Int direction = GetDirection(action, MatrixManager.Get(curMatrix), isReplay);

		return currentPosition + direction;
	}

	private Vector3Int GetDirection(PlayerAction action, MatrixInfo matrixInfo, bool isReplay)
	{
		ProcessAction(action);

		if (diagonalMovement)
		{
			return GetMoveDirection(matrixInfo, isReplay);
		}
		if (moveActionList.Count > 0)
		{
			return GetMoveDirection(moveActionList[moveActionList.Count - 1]);
		}

		return Vector3Int.zero;
	}

	private void ProcessAction(PlayerAction action)
	{
		List<int> actionKeys = new List<int>(action.moveActions);

		for (int i = 0; i < moveList.Length; i++)
		{
			if (actionKeys.Contains((int)moveList[i]) && !moveActionList.Contains(moveList[i]))
			{
				moveActionList.Add(moveList[i]);
			}
			else if (!actionKeys.Contains((int)moveList[i]) && moveActionList.Contains(moveList[i]))
			{
				moveActionList.Remove(moveList[i]);
			}
		}
	}

	private Vector3Int GetMoveDirection(MatrixInfo matrixInfo, bool isReplay)
	{
		Vector3Int direction = Vector3Int.zero;

		for (int i = 0; i < moveActionList.Count; i++)
		{
			direction += GetMoveDirection(moveActionList[i]);
		}

		direction.x = Mathf.Clamp(direction.x, -1, 1);
		direction.y = Mathf.Clamp(direction.y, -1, 1);
//			Logger.LogTrace(direction.ToString(), Category.Movement);

			if ((PlayerManager.LocalPlayer == gameObject || isServer) && !isReplay)
			{
				playerSprites.LocalFaceDirection(Orientation.From(direction.To2Int()));
			}

		if (matrixInfo.MatrixMove)
		{
			// Converting world direction to local direction
			direction = Vector3Int.RoundToInt(matrixInfo.MatrixMove.ClientState.RotationOffset.QuaternionInverted * direction);
		}

		return direction;
	}

	private Vector3Int GetMoveDirection(MoveAction action)
	{
		if (PlayerManager.LocalPlayer == gameObject && UIManager.IsInputFocus)
		{
			return Vector3Int.zero;
		}

		switch (action)
		{
			case MoveAction.MoveUp:
				return Vector3Int.up;
			case MoveAction.MoveLeft:
				return Vector3Int.left;
			case MoveAction.MoveDown:
				return Vector3Int.down;
			case MoveAction.MoveRight:
				return Vector3Int.right;
		}

		return Vector3Int.zero;
	}

	/// <summary>
	/// Restrain the player to their current position.
	/// </summary>
	/// <param name="onUnbuckled">callback to invoke when we become unbuckled</param>
	[Server]
	public void Restrain(Action onUnbuckled = null)
	{
		restrained = true;
		//can't push/pull when buckled in, break if we are pulled / pulling
		PlayerScript.pushPull.CmdStopFollowing();
		PlayerScript.pushPull.CmdStopPulling();
		PlayerScript.pushPull.isNotPushable = true;
		this.onUnbuckled = onUnbuckled;

		//if player is downed, make them upright
		if (registerPlayer.IsDownServer)
		{
			PlayerUprightMessage.SendToAll(gameObject, true, registerPlayer.IsStunnedServer);
		}
	}

	/// <summary>
	/// Unrestrain the player when they are currently restrained.
	/// </summary>
	[Command]
	public void CmdUnrestrain()
	{
		Unrestrain();
	}

	/// <summary>
	/// Server side logic for unrestraining a player
	/// </summary>
	[Server]
	public void Unrestrain()
	{
		restrained = false;
		//we can be pushed / pulled again
		PlayerScript.pushPull.isNotPushable = false;

		//if player is crit, soft crit, or dead, lay them back down
		if (playerScript.playerHealth.ConsciousState == ConsciousState.DEAD ||
		    playerScript.playerHealth.ConsciousState == ConsciousState.UNCONSCIOUS ||
		    playerScript.playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS)
		{
			PlayerUprightMessage.SendToAll(gameObject, false, registerPlayer.IsStunnedServer);
		}

		onUnbuckled?.Invoke();
	}

	//invoked client side when the restrained syncvar changes
	private void OnRestrainedChanged(bool isRestrained)
	{
		if (PlayerManager.LocalPlayer == gameObject)
		{
			//have to do this with a lambda otherwise the Cmd will not fire
			UIManager.AlertUI.ToggleAlertRestrained(isRestrained, () => this.CmdUnrestrain());
		}

		restrained = isRestrained;
	}
}