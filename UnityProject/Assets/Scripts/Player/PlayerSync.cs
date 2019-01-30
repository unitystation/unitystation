using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;



/// <summary>
/// Holds player state information used for interpolation, such as player position, flight direction etc.
/// Gives client enough information to be able to smoothly interpolate the player position.
/// </summary>
public struct PlayerState
{
	public bool Active => Position != TransformState.HiddenPos;

	public int MoveNumber;
	public Vector3 Position;

	public Vector3 WorldPosition
	{
		get
		{
			if (!Active)
			{
				return TransformState.HiddenPos;
			}

			return MatrixManager.LocalToWorld(Position, MatrixManager.Get(MatrixId));
		}
		set
		{
			if (value == TransformState.HiddenPos)
			{
				Position = TransformState.HiddenPos;
			}
			else
			{
				Position = MatrixManager.WorldToLocal(value, MatrixManager.Get(MatrixId));
			}
		}
	}

	/// Flag means that this update is a pull follow update,
	/// So that puller could ignore them
	public bool IsFollowUpdate;

	public bool NoLerp;

	///Direction of flying
	public Vector2 Impulse;

	///Flag for clients to reset their queue when received
	public bool ResetClientQueue;

	/// Flag for server to ensure that clients receive that flight update:
	/// Only important flight updates (ones with impulse) are being sent out by server (usually start only)
	[NonSerialized] public bool ImportantFlightUpdate;

	public int MatrixId;

	/// Means that this player is hidden
	public static readonly PlayerState HiddenState =
		new PlayerState { Position = TransformState.HiddenPos, MatrixId = 0 };

	public override string ToString()
	{
		return
			Equals(HiddenState) ? "[Hidden]" : $"[Move #{MoveNumber}, localPos:{(Vector2)Position}, worldPos:{(Vector2)WorldPosition} {nameof(NoLerp)}:{NoLerp}, {nameof(Impulse)}:{Impulse}, " +
			$"reset: {ResetClientQueue}, flight: {ImportantFlightUpdate}, follow: {IsFollowUpdate}, matrix #{MatrixId}]";
	}
}

public partial class PlayerSync : NetworkBehaviour, IPushable
{
	///For server code. Contains position
	public PlayerState ServerState => serverState;

	/// For client code
	public PlayerState ClientState => playerState;

		private LivingHealthBehaviour healthBehaviorScript;

	public PlayerMove playerMove;
	private PlayerScript playerScript;
	private PlayerSprites playerSprites;

	private Matrix Matrix => registerTile.Matrix;

	private RaycastHit2D[] rayHit;

	//		private float pullJourney;
	private PushPull pushPull;
	public bool IsBeingPulledServer => pushPull && pushPull.IsBeingPulled;
	public bool IsBeingPulledClient => pushPull && pushPull.IsBeingPulledClient;

	private RegisterTile registerTile;

	/// <summary>
	/// Checks both directions of a diagonal movement
	/// to see if movement or a bump resulting in an interaction is possible. Modifies
	/// the move action to switch to that direction, otherwise leaves it unmodified.
	/// Prioritizes the following when there are multiple options:
	/// 1. Move into empty space if either direction has it
	/// 2. Push an object if either direction has it
	/// 3. Open a door if either direction has it
	///
	/// When both directions have the same condition (both doors or pushable objects), x will be preferred to y
	/// </summary>
	/// <param name="state">current state to try to slide from</param>
	/// <param name="action">current player action (which should have a diagonal movement). Will be modified if a slide is performed</param>
	/// <returns>bumptype of the final direction of movement if action is modified. Null otherwise</returns>
	private BumpType? TrySlide(PlayerState state, ref PlayerAction action)
	{
		Vector2Int direction = action.Direction();
		if (Math.Abs(direction.x) + Math.Abs(direction.y) < 2)
		{
			//not diagonal, do nothing
			return null;
		}

		Vector2Int xDirection = new Vector2Int(direction.x, 0);
		Vector2Int yDirection = new Vector2Int(0, direction.y);
		BumpType xBump = MatrixManager.GetBumpTypeAt(state.WorldPosition.RoundToInt(), xDirection, gameObject);
		BumpType yBump = MatrixManager.GetBumpTypeAt(state.WorldPosition.RoundToInt(), yDirection, gameObject);

		MoveAction? newAction = null;
		BumpType? newBump = null;

		if (xBump == BumpType.None)
		{
			newAction = PlayerAction.GetMoveAction(xDirection);
			newBump = xBump;
		}
		else if (yBump == BumpType.None)
		{
			newAction = PlayerAction.GetMoveAction(yDirection);
			newBump = yBump;
		}
		else if (xBump == BumpType.Push)
		{
			newAction = PlayerAction.GetMoveAction(xDirection);
			newBump = xBump;
		}
		else if (yBump == BumpType.Push)
		{
			newAction = PlayerAction.GetMoveAction(yDirection);
			newBump = yBump;
		}
		else if (xBump == BumpType.ClosedDoor)
		{
			newAction = PlayerAction.GetMoveAction(xDirection);
			newBump = xBump;
		}
		else if (yBump == BumpType.ClosedDoor)
		{
			newAction = PlayerAction.GetMoveAction(yDirection);
			newBump = yBump;
		}

		if (newAction.HasValue)
		{
			action.moveActions = new int[] { (int)newAction };
			return newBump;
		}
		else
		{
			return null;
		}
	}

	/// <summary>
	/// Checks if any bump would occur with the movement in the specified action.
	/// If a BumpType.Blocked occurs, attempts to slide (if movement is diagonal). Updates
	/// playerAction's movement if slide occurs.
	/// </summary>
	/// <param name="playerState">state moving from</param>
	/// <param name="playerAction">action indicating the movement, will be modified if slide occurs</param>
	/// <returns>the type of bump that occurs at the final destination (after sliding has been attempted)</returns>
	private BumpType CheckSlideAndBump(PlayerState playerState, ref PlayerAction playerAction)
	{
		BumpType bump = MatrixManager.GetBumpTypeAt(playerState, playerAction, gameObject);
		// if movement is blocked, try to slide
		if (bump == BumpType.Blocked)
		{
			return TrySlide(playerState, ref playerAction) ?? bump;
		}

		return bump;
	}

	#region spess interaction logic

	private bool IsAroundPushables(PlayerState state)
	{
		PushPull pushable;
		return IsAroundPushables(state, out pushable);
	}

	/// Around player
	private bool IsAroundPushables(PlayerState state, out PushPull pushable, GameObject except = null)
	{
		return IsAroundPushables(state.WorldPosition, out pushable, except);
	}

	/// Man, these are expensive and generate a lot of garbage. Try to use sparsely
	private bool IsAroundPushables(Vector3 worldPos, out PushPull pushable, GameObject except = null)
	{
		pushable = null;
		foreach (Vector3Int pos in worldPos.CutToInt().BoundsAround().allPositionsWithin)
		{
			if (HasPushablesAt(pos, out pushable, except))
			{
				return true;
			}
		}

		return false;
	}

	private bool HasPushablesAt(Vector3 stateWorldPosition, out PushPull firstPushable, GameObject except = null)
	{
		firstPushable = null;
		var pushables = MatrixManager.GetAt<PushPull>(stateWorldPosition.CutToInt());
		if (pushables.Count == 0)
		{
			return false;
		}

		for (var i = 0; i < pushables.Count; i++)
		{
			var pushable = pushables[i];
			if (pushable.gameObject == this.gameObject || except != null && pushable.gameObject == except)
			{
				continue;
			}
			firstPushable = pushable;
			return true;
		}

		return false;
	}

	#endregion

	#region Hiding/Unhiding

	[Server]
	public void DisappearFromWorldServer()
	{
		OnPullInterrupt().Invoke();
		serverState = PlayerState.HiddenState;
		serverLerpState = PlayerState.HiddenState;
		NotifyPlayers();
	}

	[Server]
	public void AppearAtPositionServer(Vector3 worldPos)
	{
		SetPosition(worldPos);
	}

	///     Convenience method to make stuff disappear at position.
	///     For CLIENT prediction purposes.
	public void DisappearFromWorld()
	{
		playerState = PlayerState.HiddenState;
		UpdateActiveStatus();
	}

	///     Convenience method to make stuff appear at position
	///     For CLIENT prediction purposes.
	public void AppearAtPosition(Vector3 worldPos)
	{
		var pos = (Vector2)worldPos; //Cut z-axis
		playerState.MatrixId = MatrixManager.AtPoint(Vector3Int.RoundToInt(worldPos)).Id;
		playerState.WorldPosition = pos;
		transform.position = pos;
		UpdateActiveStatus();
	}

	/// Registers if unhidden, unregisters if hidden
	private void UpdateActiveStatus()
	{
		if (playerState.Active)
		{
			RegisterObjects();
		}
		else
		{
			registerTile.Unregister();
		}
		//Consider moving VisibleBehaviour functionality to CNT. Currently VB doesn't allow predictive object hiding, for example.
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = playerState.Active;
		}
	}

	#endregion

	private void Start()
	{
		//Init pending actions queue for your local player
		if (isLocalPlayer)
		{
			pendingActions = new Queue<PlayerAction>();
			UpdatePredictedState();
		}
		//Init pending actions queue for server
		if (isServer)
		{
			serverPendingActions = new Queue<PlayerAction>();
		}
		playerScript = GetComponent<PlayerScript>();
		playerSprites = GetComponent<PlayerSprites>();
		healthBehaviorScript = GetComponent<LivingHealthBehaviour>();
		registerTile = GetComponent<RegisterTile>();
		pushPull = GetComponent<PushPull>();
	}

	private void Update()
	{
		if (isLocalPlayer && playerMove != null)
		{
			//				 If being pulled by another player and you try to break free
			if (pushPull.IsBeingPulledClient && !playerScript.canNotInteract() &&
				KeyboardInputManager.IsMovementPressed())
			{
				pushPull.CmdStopFollowing();
				return;
			}
			if (ClientPositionReady && !playerMove.isGhost
			   || GhostPositionReady && playerMove.isGhost)
			{
				DoAction();
			}
		}

		Synchronize();
	}

	private void RegisterObjects()
	{
		//Register playerpos in matrix
		registerTile.UpdatePosition();
		//Registering objects being pulled in matrix
		//			if ( pullRegister != null ) {
		//				pullRegister.UpdatePosition();
		//			}
	}

	private void Synchronize()
	{
		if (isLocalPlayer && GameData.IsHeadlessServer)
		{
			return;
		}

		if (Matrix != null)
		{
			CheckMovementClient();
			bool server = isServer;
			if (server)
			{
				CheckMovementServer();
			}
			if (!ClientPositionReady)
			{
				Lerp();
			}
			if (server)
			{
				if (Input.GetKeyDown(KeyCode.F7) && gameObject == PlayerManager.LocalPlayer)
				{
					SpawnHandler.SpawnDummyPlayer(JobType.ASSISTANT);
				}
				if (serverState.Position != serverLerpState.Position)
				{
					ServerLerp();
				}
				else
				{
					TryUpdateServerTarget();
				}

			}
		}

		//Registering
		if (registerTile.Position != Vector3Int.RoundToInt(predictedState.Position))
		{
			RegisterObjects();
		}
	}

	public void OnBecomeGhost()
	{
		playerScript.ghost.transform.position = playerState.WorldPosition;
		ghostPredictedState = playerState;
	}

	private void GhostLerp(PlayerState state)
	{
		playerScript.ghost.transform.position =
			Vector3.MoveTowards(playerScript.ghost.transform.position, state.WorldPosition,
				playerMove.speed * Time.deltaTime * playerScript.ghost.transform.position.SpeedTo(state.WorldPosition));
	}

	private PlayerState NextState(PlayerState state, PlayerAction action, out bool matrixChanged, bool isReplay = false)
	{
		var newState = state;
		newState.MoveNumber++;
		newState.Position = playerMove.GetNextPosition(Vector3Int.RoundToInt(state.Position), action, isReplay, MatrixManager.Get(newState.MatrixId).Matrix);

		var proposedWorldPos = newState.WorldPosition;
		MatrixInfo matrixAtPoint = MatrixManager.AtPoint(Vector3Int.RoundToInt(proposedWorldPos));
		bool matrixChangeDetected = !Equals(matrixAtPoint, MatrixInfo.Invalid) && matrixAtPoint.Id != state.MatrixId;

		//Switching matrix while keeping world pos
		newState.MatrixId = matrixAtPoint.Id;
		newState.WorldPosition = proposedWorldPos;

		//			Logger.Log( $"NextState: src={state} proposedPos={newState.WorldPosition}\n" +
		//			           $"mAtPoint={matrixAtPoint.Id} change={matrixChangeDetected} newState={newState}" );

		if (!matrixChangeDetected)
		{
			matrixChanged = false;
			return newState;
		}

		matrixChanged = true;
		return newState;
	}


	public void ProcessAction(PlayerAction action)
	{
		CmdProcessAction(action);
	}

#if UNITY_EDITOR
	//Visual debug
	[NonSerialized]
	private readonly Vector3 size1 = Vector3.one,
							 size2 = new Vector3(0.9f, 0.9f, 0.9f),
							 size3 = new Vector3(0.8f, 0.8f, 0.8f),
							 size4 = new Vector3(0.7f, 0.7f, 0.7f),
							 size5 = new Vector3(1.1f, 1.1f, 1.1f),
							 size6 = new Vector3(0.6f, 0.6f, 0.6f);
	[NonSerialized]
	private readonly Color color0 = DebugTools.HexToColor("5566ff55"),//blue
							color1 = Color.red,
							color2 = DebugTools.HexToColor("fd7c6e"),//pink
							color3 = DebugTools.HexToColor("22e600"),//green
							color4 = DebugTools.HexToColor("ebfceb"),//white
							color6 = DebugTools.HexToColor("666666");//grey
	private static readonly bool drawMoves = true;

	private void OnDrawGizmos()
	{
		//registerTile pos
		Gizmos.color = color0;
		Vector3 regPos = registerTile.WorldPosition;
		Gizmos.DrawCube(regPos, size5);

		//serverState
		Gizmos.color = color1;
		Vector3 stsPos = serverState.WorldPosition;
		Gizmos.DrawWireCube(stsPos, size1);
		GizmoUtils.DrawArrow(stsPos + Vector3.left / 2, serverState.Impulse);
		if (drawMoves) GizmoUtils.DrawText(serverState.MoveNumber.ToString(), stsPos + Vector3.left / 4, 15);

		//serverLerpState
		Gizmos.color = color2;
		Vector3 ssPos = serverLerpState.WorldPosition;
		Gizmos.DrawWireCube(ssPos, size2);
		GizmoUtils.DrawArrow(ssPos + Vector3.right / 2, serverLerpState.Impulse);
		if (drawMoves) GizmoUtils.DrawText(serverLerpState.MoveNumber.ToString(), ssPos + Vector3.right / 4, 15);

		//client predictedState
		Gizmos.color = color3;
		Vector3 clientPrediction = predictedState.WorldPosition;
		Gizmos.DrawWireCube(clientPrediction, size3);
		GizmoUtils.DrawArrow(clientPrediction + Vector3.left / 5, predictedState.Impulse);
		if (drawMoves) GizmoUtils.DrawText(predictedState.MoveNumber.ToString(), clientPrediction + Vector3.left, 15);

		//client ghostState
		Gizmos.color = color6;
		Vector3 ghostPrediction = ghostPredictedState.WorldPosition;
		Gizmos.DrawWireCube(ghostPrediction, size6);
		//			GizmoUtils.DrawArrow( ghostPrediction + Vector3.left / 5, predictedState.Impulse );
		if (drawMoves) GizmoUtils.DrawText(ghostPredictedState.MoveNumber.ToString(), ghostPrediction + Vector3.up / 2, 15);

		//client playerState
		Gizmos.color = color4;
		Vector3 clientState = playerState.WorldPosition;
		Gizmos.DrawWireCube(clientState, size4);
		GizmoUtils.DrawArrow(clientState + Vector3.right / 5, playerState.Impulse);
		if (drawMoves) GizmoUtils.DrawText(playerState.MoveNumber.ToString(), clientState + Vector3.right, 15);

	}
#endif
}
