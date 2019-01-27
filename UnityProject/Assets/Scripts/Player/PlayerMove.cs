using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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
		[SyncVar] public bool isGhost;

		private readonly List<MoveAction> moveActionList = new List<MoveAction>();

		public MoveAction[] moveList =
		{
			MoveAction.MoveUp, MoveAction.MoveLeft, MoveAction.MoveDown, MoveAction.MoveRight
		};

		private PlayerSprites playerSprites;

		[HideInInspector] public PlayerNetworkActions pna;
		public float speed = 10;

		private RegisterTile registerTile;
		private Matrix matrix => registerTile.Matrix;

		/// temp solution for use with the UI network prediction
		public bool isMoving { get; } = false;

		private void Start()
		{
			playerSprites = gameObject.GetComponent<PlayerSprites>();
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

				// if (Input.GetKey(moveList[i]) && allowInput)
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
				playerSprites.FaceDirection(Orientation.From(direction.To2Int()));
			}

			if (matrixInfo.MatrixMove)
			{
				// Converting world direction to local direction
				direction = Vector3Int.RoundToInt(matrixInfo.MatrixMove.ClientState.RotationOffset.EulerInverted * direction);
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
		///     Check current and next tiles to determine their status and if movement is allowed
		/// </summary>
		private Vector3Int AdjustDirection(Vector3Int currentPosition, Vector3Int direction, bool isReplay, Matrix curMatrix)
		{ //TODO: no longer used, remove after pulling is in
			if (isGhost)
			{
				return direction;
			}

			Vector3Int newPos = currentPosition + direction;

			// isReplay tells AdjustDirection if the move being carried out is a replay move for prediction or not
			// a replay move is a move that has already been carried out on the LocalPlayer's client
			if (!isReplay)
			{
				// Check the high level matrix detector
//				if (!MatrixManager.CanPass(currentPosition, direction, curMatrix))
//				{
//					Logger.LogError( $"Why the hell did this trigger? localPos={currentPosition}+{Orientation.From( direction )} on {curMatrix}", Category.Movement );
//				}

//				// Not to be checked while performing a replay:
//				if (playerSync.PullingObject != null)
//				{
//					if (curMatrix.ContainsAt(newPos, playerSync.PullingObject))
//					{
//						//Vector2 directionToPullObj =
//						//	playerSync.pullingObject.transform.localPosition - transform.localPosition;
//						//if (directionToPullObj.normalized != playerSprites.currentDirection) {
//						//	// Ran into pullObject but was not facing it, saved direction
//						//	return direction;
//						//}
//						//Hit Pull obj
//						pna.CmdStopPulling(playerSync.PullingObject);
//
//						return Vector3Int.zero;
//					}
//				}
			}

			if (!curMatrix.ContainsAt(newPos, gameObject) && curMatrix.IsPassableAt(currentPosition, newPos) && !isReplay)
			{
				return direction;
			}

//			// This is only for replay (to ignore any interactions with the pulled obj):
//			if (playerSync.PullingObject != null)
//			{
//				if (curMatrix.ContainsAt(newPos, playerSync.PullingObject))
//				{
//					return direction;
//				}
//			}

			if (isReplay)
			{
				return direction;
			}

			// Could not pass
			// Logger.Log("Couldn't pass");
			return Vector3Int.zero;

		}



	}
