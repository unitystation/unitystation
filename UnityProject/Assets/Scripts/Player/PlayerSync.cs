using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public partial class PlayerSync : NetworkBehaviour, IPushable, IPlayerControllable
{
	public bool VisibleState {
		get => ServerPosition != TransformState.HiddenPos;
		set => SetVisibleServer( value );
	}

	/// <summary>
	/// Player is huge, okay?
	/// </summary>
	public ItemSize Size => ItemSize.Huge;

	public bool IsTileSnap { get; } = true;

	///For server code. Contains position
	public PlayerState ServerState
	{
		get => serverState;
		private set
		{
			if ( serverState.LastNonHiddenPosition == Vector3.zero )
			{
				serverState = value;
			} else
			{
				var preservedPos = serverState.LastNonHiddenPosition;
				serverState = value;
				serverState.LastNonHiddenPosition = preservedPos;
			}
		}
	}

	/// For client code
	public PlayerState ClientState => playerState;

	/// <summary>
	/// Returns whether player is currently moving. Returns correct value depending on if this
	/// is being called from client or server.
	/// </summary>
	public bool IsMoving => isServer ? IsMovingServer : IsMovingClient;

	public PlayerMove playerMove;
	private PlayerScript playerScript;
	private Directional playerDirectional;

	private Matrix Matrix => registerPlayer != null ? registerPlayer.Matrix : null;

	private RaycastHit2D[] rayHit;

	//		private float pullJourney;
	/// <summary>
	/// Note - can be null if this is a ghost player
	/// </summary>
	private PushPull pushPull;

	public bool IsBeingPulledServer => pushPull && pushPull.IsBeingPulled;
	public bool IsBeingPulledClient => pushPull && pushPull.IsBeingPulledClient;

	public void Nudge(NudgeInfo info)
	{
	}

	/// <summary>
	/// Checks both directions of a diagonal movement
	/// to see if movement or a bump resulting in an interaction is possible. Modifies
	/// the move action to switch to that direction, otherwise leaves it unmodified.
	/// Prioritizes the following when there are multiple options:
	/// 1. Move into empty space if either direction has it
	/// 2. Swap with a player if we and they have help intent
	/// 3. Push an object if either direction has it
	/// 4. Open a door if either direction has it
	///
	/// When both directions have the same condition (both doors or pushable objects), x will be preferred to y
	/// </summary>
	/// <param name="state">current state to try to slide from</param>
	/// <param name="action">current player action (which should have a diagonal movement). Will be modified if a slide is performed</param>
	/// <returns>bumptype of the final direction of movement if action is modified. Null otherwise</returns>
	private BumpType? TrySlide(PlayerState state, bool isServer, ref PlayerAction action)
	{
		Vector2Int direction = action.Direction();
		if (Math.Abs(direction.x) + Math.Abs(direction.y) < 2)
		{
			//not diagonal, do nothing
			return null;
		}

		var facingUpDown = playerDirectional.CurrentDirection == Orientation.Up ||
		                   playerDirectional.CurrentDirection == Orientation.Down;

		//depending on facing, check x / y direction first (this is for
		//better diagonal movement logic without cutting corners)
		Vector2Int dir1 = new Vector2Int(facingUpDown ? direction.x : 0, facingUpDown ? 0 : direction.y);
		Vector2Int dir2 = new Vector2Int(facingUpDown ? 0 : direction.x, facingUpDown ? direction.y : 0);
		BumpType bump1 = MatrixManager.GetBumpTypeAt(state.WorldPosition.RoundToInt(), dir1, playerMove, isServer);
		BumpType bump2 = MatrixManager.GetBumpTypeAt(state.WorldPosition.RoundToInt(), dir2, playerMove, isServer);

		MoveAction? newAction = null;
		BumpType? newBump = null;

		if (bump1 == BumpType.None || bump1 == BumpType.Swappable)
		{
			newAction = PlayerAction.GetMoveAction(dir1);
			newBump = bump1;
		}
		else if (bump2 == BumpType.None || bump2 == BumpType.Swappable)
		{
			newAction = PlayerAction.GetMoveAction(dir2);
			newBump = bump2;
		}
		else if (bump1 == BumpType.Push)
		{
			newAction = PlayerAction.GetMoveAction(dir1);
			newBump = bump1;
		}
		else if (bump2 == BumpType.Push)
		{
			newAction = PlayerAction.GetMoveAction(dir2);
			newBump = bump2;
		}
		else if (bump1 == BumpType.ClosedDoor)
		{
			newAction = PlayerAction.GetMoveAction(dir1);
			newBump = bump1;
		}
		else if (bump2 == BumpType.ClosedDoor)
		{
			newAction = PlayerAction.GetMoveAction(dir2);
			newBump = bump2;
		}

		if (newAction.HasValue)
		{
			action.moveActions = new int[] {(int) newAction};
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
	private BumpType CheckSlideAndBump(PlayerState playerState, bool isServer, ref PlayerAction playerAction)
	{
		//bump never happens if we are a ghost
		if (playerScript.IsGhost)
		{
			return BumpType.None;
		}

		BumpType bump = MatrixManager.GetBumpTypeAt(playerState, playerAction, playerMove, isServer);
		//on diagonal movement, don't allow cutting corners or pushing (check x and y tile passability)
		var dir = playerAction.Direction();
		if (dir.x != 0 && dir.y != 0)
		{
			if (bump == BumpType.Push)
			{
				bump = BumpType.Blocked;
			}

			var xBump = MatrixManager.GetBumpTypeAt(playerState.WorldPosition.RoundToInt(), new Vector2Int(dir.x, 0),
				playerMove, isServer);
			var yBump = MatrixManager.GetBumpTypeAt(playerState.WorldPosition.RoundToInt(), new Vector2Int(0, dir.y),
				playerMove, isServer);

			//opening doors diagonally is allowed only if x or y are blocked (assumes we are sliding along a
			//wall and we hit a door).
			//if both are open, then we will instead slide to one of the open spaces.
			//This gives better behavior on opening side by side doors while sliding on a wall
			if ((xBump == BumpType.Blocked || yBump == BumpType.Blocked) && bump != BumpType.ClosedDoor)
			{
				bump = BumpType.Blocked;
			}
			else if (xBump != BumpType.Blocked && yBump != BumpType.Blocked && bump == BumpType.ClosedDoor)
			{
				bump = BumpType.Blocked;
			}
		}

		// if movement is blocked, try to slide
		if (bump == BumpType.Blocked)
		{
			return TrySlide(playerState, isServer, ref playerAction) ?? bump;
		}

		return bump;
	}


	#region spess interaction logic

	private bool IsAroundPushables(PlayerState state, bool isServer)
	{
		PushPull pushable;
		return IsAroundPushables(state, isServer, out pushable);
	}

	/// Around player
	private bool IsAroundPushables(PlayerState state, bool isServer, out PushPull pushable, GameObject except = null)
	{
		return IsAroundPushables(state.WorldPosition, isServer, out pushable, except);
	}

	/// Man, these are expensive and generate a lot of garbage. Try to use sparsely
	private bool IsAroundPushables(Vector3 worldPos, bool isServer, out PushPull pushable, GameObject except = null)
	{
		pushable = null;
		foreach (Vector3Int pos in worldPos.CutToInt().BoundsAround().allPositionsWithin)
		{
			if (HasPushablesAt(pos, isServer, out pushable, except))
			{
				return true;
			}
		}

		return false;
	}

	private bool HasPushablesAt(Vector3 stateWorldPosition, bool isServer, out PushPull firstPushable,
		GameObject except = null)
	{
		firstPushable = null;
		var pushables = MatrixManager.GetAt<PushPull>(stateWorldPosition.CutToInt(), isServer);
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
		ServerState = PlayerState.HiddenState;
		serverLerpState = PlayerState.HiddenState;
		NotifyPlayers(true);
	}

	[Server]
	public void AppearAtPositionServer(Vector3 worldPos)
	{
		SetPosition(worldPos, true);
	}

	#endregion

	#region swapping positions

	/// <summary>
	/// Checks if a swap would occur due to moving to a position with a player with help intent while
	/// we have help intent and are not dragging something.
	/// If so, performs the swap by shifting the other player - this method doesn't affect our own movement/position,
	/// only the swapee is affected.
	/// </summary>
	/// <param name="targetWorldPos">target position being moved to to check for a swap at</param>
	/// <param name="inDirection">direction to which the swapee should be moved if swap occurs (should
	/// be opposite the direction of this player's movement)</param>
	/// <returns>true iff swap was performed</returns>
	private bool CheckAndDoSwap(Vector3Int targetWorldPos, Vector2 inDirection, bool isServer)
	{
		PlayerMove other = MatrixManager.GetSwappableAt(targetWorldPos, gameObject, isServer);
		if (other != null)
		{
			// on server, must verify that position matches
			if ((isServer && !other.PlayerScript.PlayerSync.IsMovingServer)
			    || (!isServer && !other.PlayerScript.PlayerSync.IsMovingClient))
			{
				//they've stopped there, so let's swap them
				InitiateSwap(other, targetWorldPos + inDirection.RoundToInt());
				return true;
			}
		}

		return false;
	}


	/// <summary>
	/// Invoked when someone is swapping positions with us due to arriving on our space when we have help intent.
	///
	/// Invoked on client for client prediction, server for server-authoritative logic.
	///
	/// This player is the swapee, the person displacing us is the swapper.
	/// </summary>
	/// <param name="toWorldPosition">destination to move to</param>
	/// <param name="swapper">pushpull of the person initiating the swap, to check if we should break our
	/// current pull</param>
	private void BeSwapped(Vector3Int toWorldPosition, PushPull swapper)
	{
		if (isServer)
		{
			Logger.LogFormat("Swap {0} from {1} to {2}", Category.Lerp, name, (Vector2) serverState.WorldPosition,
				toWorldPosition.To2Int());
			PlayerState nextStateServer = NextStateSwap(serverState, toWorldPosition, true);
			ServerState = nextStateServer;
			if (pushPull != null && pushPull.IsBeingPulled && !pushPull.PulledBy == swapper)
			{
				pushPull.StopFollowing();
			}
		}

		PlayerState nextPredictedState = NextStateSwap(predictedState, toWorldPosition, false);
		//must set this on both client and server so server shows the lerp instantly as well as the client
		predictedState = nextPredictedState;
	}

	/// <summary>
	/// Called on clientside for prediction and server side for server-authoritative logic.
	///
	/// Swap the other player, sending them in direction (which should be opposite our direction of motion).
	///
	/// Doesn't affect this player's movement/position, only the swapee is affected.
	///
	/// This player is the swapper, the one they are displacing is the swapee
	/// </summary>
	/// <param name="swapee">player we will swap</param>
	/// <param name="toWorldPosition">destination to swap to</param>
	private void InitiateSwap(PlayerMove swapee, Vector3Int toWorldPosition)
	{
		swapee.PlayerScript.PlayerSync.BeSwapped(toWorldPosition, pushPull);
	}

	#endregion

	private void Awake()
	{
		playerScript = GetComponent<PlayerScript>();
		pushPull = GetComponent<PushPull>();
		playerDirectional = GetComponent<Directional>();
		registerPlayer = GetComponent<RegisterPlayer>();
	}

	public override void OnStartClient()
	{
		//prevents player temporarily showing up at 0,0 when they spawn before they receive their first position
		playerState.WorldPosition = transform.localPosition;
		PlayerNewPlayer.Send(netId);
	}

	public override void OnStartServer()
	{
		serverPendingActions = new Queue<PlayerAction>();
		InitServerState();
	}

	public override void OnStartLocalPlayer()
	{
		setLocalPlayer();
	}

	private void OnEnable()
	{
		onTileReached.AddListener(Cross);
		EventManager.AddHandler(EVENT.PlayerRejoined, setLocalPlayer);
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}
	private void OnDisable()
	{
		onTileReached.RemoveListener(Cross);
		EventManager.RemoveHandler(EVENT.PlayerRejoined, setLocalPlayer);
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	/// <summary>
	/// Sets up the action queue for the local player.
	/// </summary>
	public void setLocalPlayer()
	{
		if (isLocalPlayer)
		{
			pendingActions = new Queue<PlayerAction>();
			UpdatePredictedState();
			predictedSpeedClient = UIManager.WalkRun.running ? playerMove.RunSpeed : playerMove.WalkSpeed;
		}
	}

	/// <summary>
	/// true when player tries to break pull or leave locker.
	/// </summary>
	private bool didWiggle = false;

	private void UpdateMe()
	{
		if (isLocalPlayer && playerMove != null)
		{
			if (PlayerManager.MovementControllable == this)
			{
				didWiggle = false;
				if (KeyboardInputManager.IsMovementPressed() && Validations.CanInteract(playerScript,
					    isServer ? NetworkSide.Server : NetworkSide.Client))
				{
					//	If being pulled by another player and you try to break free
					if (pushPull != null && pushPull.IsBeingPulledClient)
					{
						pushPull.CmdStopFollowing();
						didWiggle = true;
					}
					else if (Camera2DFollow.followControl.target != PlayerManager.LocalPlayer.transform)
					{
						//	Leaving locker
						var closet = Camera2DFollow.followControl.target.GetComponent<ClosetControl>();
						if (closet)
						{
							InteractionUtils.RequestInteract(HandApply.ByLocalPlayer(closet.gameObject), closet);
							didWiggle = true;
						}
					}
				}
			}
		}

		Synchronize();

		//experimental: if buckled, no matter what happens, draw us on top of our buckled object
		if (playerMove.IsBuckled)
		{
			transform.position = playerMove.BuckledObject.transform.position;
		}
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
				if (CommonInput.GetKeyDown(KeyCode.F7) && gameObject == PlayerManager.LocalPlayer)
				{
					PlayerSpawn.ServerSpawnDummy();
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
		if (registerPlayer.LocalPositionClient != Vector3Int.RoundToInt(predictedState.Position))
		{
			registerPlayer.UpdatePositionClient();
		}

		if (registerPlayer.LocalPositionServer != Vector3Int.RoundToInt(serverState.Position))
		{
			registerPlayer.UpdatePositionServer();
		}
	}

	/// <summary>
	/// Transition to next state for a swap, modifying parent matrix if matrix change occurs but not
	/// incrementing the movenumber.
	/// </summary>
	/// <param name="state">current state</param>
	/// <param name="toWorldPosition">world position new state should be in</param>
	/// <returns>state with worldposition as its worldposition, changing the parent matrix if a matrix change occurs</returns>
	private PlayerState NextStateSwap(PlayerState state, Vector3Int toWorldPosition, bool isServer)
	{
		var newState = state;
		newState.WorldPosition = toWorldPosition;

		MatrixInfo matrixAtPoint = MatrixManager.AtPoint(toWorldPosition, isServer);

		//Switching matrix while keeping world pos
		newState.MatrixId = matrixAtPoint.Id;
		newState.WorldPosition = toWorldPosition;

		return newState;
	}

	private PlayerState NextState(PlayerState state, PlayerAction action, bool isServer, bool isReplay = false)
	{
		var newState = state;
		newState.MoveNumber++;
		newState.Position = playerMove.GetNextPosition(Vector3Int.RoundToInt(state.Position), action, isReplay,
			MatrixManager.Get(newState.MatrixId).Matrix);

		var proposedWorldPos = newState.WorldPosition;

		MatrixInfo matrixAtPoint = MatrixManager.AtPoint(Vector3Int.RoundToInt(proposedWorldPos), isServer);

		//Switching matrix while keeping world pos
		newState.MatrixId = matrixAtPoint.Id;
		newState.WorldPosition = proposedWorldPos;

		return newState;
	}

	public void ProcessAction(PlayerAction action)
	{
		CmdProcessAction(action);
	}

#if UNITY_EDITOR
	//Visual debug
	[NonSerialized] private readonly Vector3 size1 = Vector3.one,
		size2 = new Vector3(0.9f, 0.9f, 0.9f),
		size3 = new Vector3(0.8f, 0.8f, 0.8f),
		size4 = new Vector3(0.7f, 0.7f, 0.7f),
		size5 = new Vector3(1.1f, 1.1f, 1.1f),
		size6 = new Vector3(0.6f, 0.6f, 0.6f);

	[NonSerialized] private readonly Color color0 = DebugTools.HexToColor("5566ff55"), //blue
		color1 = Color.red,
		color2 = DebugTools.HexToColor("fd7c6e"), //pink
		color3 = DebugTools.HexToColor("22e600"), //green
		color4 = DebugTools.HexToColor("ebfceb"), //white
		color6 = DebugTools.HexToColor("666666"), //grey
		color7 = DebugTools.HexToColor("ff666655"); //reddish

	private static readonly bool drawMoves = true;

	private void OnDrawGizmos()
	{
		if (!Application.isPlaying) return;

		//registerTile S pos
		Gizmos.color = color7;
		Vector3 regPosS = registerPlayer.WorldPositionServer;
		Gizmos.DrawCube(regPosS, size5);

		//registerTile C pos
		Gizmos.color = color0;
		Vector3 regPosC = registerPlayer.WorldPositionClient;
		Gizmos.DrawCube(regPosC, size2);

		//serverState
		Gizmos.color = color1;
		Vector3 stsPos = serverState.WorldPosition;
		Gizmos.DrawWireCube(stsPos, size1);
		DebugGizmoUtils.DrawArrow(stsPos + Vector3.left / 2, serverState.WorldImpulse);
		if (drawMoves) DebugGizmoUtils.DrawText(serverState.MoveNumber.ToString(), stsPos + Vector3.left / 4, 15);

		//serverLerpState
		Gizmos.color = color2;
		Vector3 ssPos = serverLerpState.WorldPosition;
		Gizmos.DrawWireCube(ssPos, size2);
		DebugGizmoUtils.DrawArrow(ssPos + Vector3.right / 2, serverLerpState.WorldImpulse);
		if (drawMoves) DebugGizmoUtils.DrawText(serverLerpState.MoveNumber.ToString(), ssPos + Vector3.right / 4, 15);

		//client predictedState
		Gizmos.color = color3;
		Vector3 clientPrediction = predictedState.WorldPosition;
		Gizmos.DrawWireCube(clientPrediction, size3);
		DebugGizmoUtils.DrawArrow(clientPrediction + Vector3.left / 5, predictedState.WorldImpulse);
		if (drawMoves)
			DebugGizmoUtils.DrawText(predictedState.MoveNumber.ToString(), clientPrediction + Vector3.left, 15);

		//client playerState
		Gizmos.color = color4;
		Vector3 clientState = playerState.WorldPosition;
		Gizmos.DrawWireCube(clientState, size4);
		DebugGizmoUtils.DrawArrow(clientState + Vector3.right / 5, playerState.WorldImpulse);
		if (drawMoves) DebugGizmoUtils.DrawText(playerState.MoveNumber.ToString(), clientState + Vector3.right, 15);

		//swappable
		Gizmos.color = isLocalPlayer ? color4 : color1;
		if (playerMove.IsSwappable)
		{
			DebugGizmoUtils.DrawText("Swap", clientState + Vector3.up / 2, 15);
		}
	}
#endif

	public void RecievePlayerMoveAction(PlayerAction moveActions)
	{
		if (moveActions.moveActions.Length != 0 && !MoveCooldown
		                                        && isLocalPlayer && playerMove != null
		                                        && !didWiggle && ClientPositionReady)
		{
			bool beingDraggedWithCuffs = playerMove.IsCuffed && playerScript.pushPull.IsBeingPulledClient;

			if (playerMove.allowInput && !playerMove.IsBuckled && !beingDraggedWithCuffs && !UIManager.IsInputFocus)
			{
				StartCoroutine(DoProcess(moveActions));
			}
			else // Player tried to move but isn't allowed
			{
				if (!playerScript.IsGhost && playerScript.playerHealth.IsDead)
				{
					playerScript.playerNetworkActions.CmdSpawnPlayerGhost();
				}
			}
		}
	}
}
