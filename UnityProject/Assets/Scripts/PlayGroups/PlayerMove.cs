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
		public bool azerty;

		[SyncVar] public bool allowInput = true;
		[SyncVar] public bool isGhost;

		private readonly List<KeyCode> pressedKeys = new List<KeyCode>();

		public KeyCode[] keyCodes =
		{
			KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow,
			KeyCode.RightArrow
		};

		private PlayerSprites playerSprites;
		private PlayerSync playerSync;

		[HideInInspector] public PlayerNetworkActions pna;
//		[HideInInspector] public PushPull pushPull; //The push pull component attached to this player
		public float speed = 10;

//		public bool IsPushing { get; set; }

		private RegisterTile registerTile;
		private Matrix matrix => registerTile.Matrix;

		/// temp solution for use with the UI network prediction
		public bool isMoving { get; } = false;

		private void Start()
		{
			playerSprites = gameObject.GetComponent<PlayerSprites>();
			playerSync = GetComponent<PlayerSync>();
//			pushPull = GetComponent<PushPull>();
			registerTile = GetComponent<RegisterTile>();
			pna = gameObject.GetComponent<PlayerNetworkActions>();
		}

		public PlayerAction SendAction()
		{
			List<int> actionKeys = new List<int>();

			for (int i = 0; i < keyCodes.Length; i++)
			{
				if (PlayerManager.LocalPlayer == gameObject && UIManager.IsInputFocus)
				{
					return new PlayerAction { keyCodes = actionKeys.ToArray() };
				}

				if (Input.GetKey(keyCodes[i]) && allowInput /*&& !IsPushing*/)
				{
					actionKeys.Add((int)keyCodes[i]);
				}
			}

			return new PlayerAction { keyCodes = actionKeys.ToArray() };
		}

		public Vector3Int GetNextPosition(Vector3Int currentPosition, PlayerAction action, bool isReplay, Matrix curMatrix = null)
		{
			if (!curMatrix)
			{
				curMatrix = matrix;
			}

			Vector3Int direction = GetDirection(action, MatrixManager.Get(curMatrix));
//			Vector3Int adjustedDirection = AdjustDirection(currentPosition, direction, isReplay, curMatrix);

//			if (adjustedDirection == Vector3.zero && !isReplay)
//			{
//				Interact(currentPosition, direction);
//			}

			return currentPosition + direction;
		}

		public string ChangeKeyboardInput(bool setAzerty)
		{
			ControlAction controlAction = UIManager.Action;

			if (setAzerty)
			{
				keyCodes = new KeyCode[] { KeyCode.Z, KeyCode.Q, KeyCode.S, KeyCode.D, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.RightArrow };
				azerty = true;
				controlAction.azerty = true;
				PlayerPrefs.SetInt("AZERTY", 1);
				PlayerPrefs.Save();

				return "AZERTY";
			}

			keyCodes = new KeyCode[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.RightArrow };
			azerty = false;
			controlAction.azerty = false;
			PlayerPrefs.SetInt("AZERTY", 0);

			return "QWERTY";
		}

		private Vector3Int GetDirection(PlayerAction action, MatrixInfo matrixInfo)
		{
			ProcessAction(action);

			if (diagonalMovement)
			{
				return GetMoveDirection(matrixInfo);
			}
			if (pressedKeys.Count > 0)
			{
				return GetMoveDirection(pressedKeys[pressedKeys.Count - 1]);
			}

			return Vector3Int.zero;
		}

		private void ProcessAction(PlayerAction action)
		{
			List<int> actionKeys = new List<int>(action.keyCodes);

			for (int i = 0; i < keyCodes.Length; i++)
			{
				if (actionKeys.Contains((int)keyCodes[i]) && !pressedKeys.Contains(keyCodes[i]))
				{
					pressedKeys.Add(keyCodes[i]);
				}
				else if (!actionKeys.Contains((int)keyCodes[i]) && pressedKeys.Contains(keyCodes[i]))
				{
					pressedKeys.Remove(keyCodes[i]);
				}
			}
		}

		private Vector3Int GetMoveDirection(MatrixInfo matrixInfo)
		{
			Vector3Int direction = Vector3Int.zero;

			for (int i = 0; i < pressedKeys.Count; i++)
			{
				direction += GetMoveDirection(pressedKeys[i]);
			}

			direction.x = Mathf.Clamp(direction.x, -1, 1);
			direction.y = Mathf.Clamp(direction.y, -1, 1);
//			Logger.LogTrace(direction.ToString(), Category.Movement);

			if (!isGhost && PlayerManager.LocalPlayer == gameObject)
			{
				playerSprites.CmdChangeDirection(Orientation.From(direction));
				// Prediction:
				playerSprites.FaceDirection(Orientation.From(direction));
			}

			if (matrixInfo.MatrixMove)
			{
				// Converting world direction to local direction
				direction = Vector3Int.RoundToInt(matrixInfo.MatrixMove.ClientState.Orientation.EulerInverted * direction);
			}

			return direction;
		}

		private Vector3Int GetMoveDirection(KeyCode action)
		{
			if (PlayerManager.LocalPlayer == gameObject && UIManager.IsInputFocus)
			{
				return Vector3Int.zero;
			}

			// @TODO This needs a refactor, but this way AZERTY will work without weird conflicts.
			if (azerty)
			{
				switch (action)
				{
					case KeyCode.Z:
					case KeyCode.UpArrow:
						return Vector3Int.up;
					case KeyCode.Q:
					case KeyCode.LeftArrow:
						return Vector3Int.left;
					case KeyCode.S:
					case KeyCode.DownArrow:
						return Vector3Int.down;
					case KeyCode.D:
					case KeyCode.RightArrow:
						return Vector3Int.right;
				}
			}
			else
			{
				switch (action)
				{
					case KeyCode.W:
					case KeyCode.UpArrow:
						return Vector3Int.up;
					case KeyCode.A:
					case KeyCode.LeftArrow:
						return Vector3Int.left;
					case KeyCode.S:
					case KeyCode.DownArrow:
						return Vector3Int.down;
					case KeyCode.D:
					case KeyCode.RightArrow:
						return Vector3Int.right;
				}
			}

			return Vector3Int.zero;
		}

		/// <summary>
		///     Check current and next tiles to determine their status and if movement is allowed
		/// </summary>
		private Vector3Int AdjustDirection(Vector3Int currentPosition, Vector3Int direction, bool isReplay, Matrix curMatrix)
		{
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
