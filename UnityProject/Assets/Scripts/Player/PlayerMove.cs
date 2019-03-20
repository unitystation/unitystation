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

	private RegisterTile registerTile;
	private Matrix matrix => registerTile.Matrix;

	/// temp solution for use with the UI network prediction
	public bool isMoving { get; } = false;

	private void Start()
	{
		playerSprites = gameObject.GetComponent<UserControlledSprites>();

		registerTile = GetComponent<RegisterTile>();
		pna = gameObject.GetComponent<PlayerNetworkActions>();
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
			if (KeyboardInputManager.CheckMoveAction(moveList[i]) && allowInput)
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
}