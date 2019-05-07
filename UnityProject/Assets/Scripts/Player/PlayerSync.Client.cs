using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class PlayerSync
{
	//Client-only fields, don't concern server
	private DualVector3IntEvent onClientStartMove = new DualVector3IntEvent();
	public DualVector3IntEvent OnClientStartMove() => onClientStartMove;
	private Vector3IntEvent onClientTileReached = new Vector3IntEvent();
	public Vector3IntEvent OnClientTileReached()
	{
		return onClientTileReached;
	}

	/// Trusted state, received from server
	private PlayerState playerState;

	/// Client predicted state
	private PlayerState predictedState;

	private Queue<PlayerAction> pendingActions;
	private Vector2 lastDirection;

	/// Last move direction, used for space walking simulation
	private Vector2 LastDirection
	{
		get { return lastDirection; }
		set
		{
			//				if ( value == Vector2.zero ) {
			//					Logger.Log( $"Setting client LastDirection to {value}!" );
			//				}
			lastDirection = value;
		}
	}
	public bool CanPredictPush => ClientPositionReady;
	public bool IsMovingClient => !ClientPositionReady;
	public Vector3Int ClientPosition => predictedState.WorldPosition.RoundToInt();
	public Vector3Int ClientLocalPosition => predictedState.Position.RoundToInt();
	public Vector3Int TrustedPosition => playerState.WorldPosition.RoundToInt();
	public Vector3Int TrustedLocalPosition => playerState.Position.RoundToInt();

	/// Does client's transform pos match state pos? Ignores Z-axis.
	private bool ClientPositionReady => (Vector2)predictedState.Position == (Vector2)transform.localPosition;

	// Is true on the frame that the player is bumping into something
	public bool isBumping;
	private bool IsWeightlessClient
	{
		get
		{
			if (playerScript.IsGhost)
			{
				return false;
			}
			GameObject[] context = pushPull.IsPullingSomethingClient ? new[] { gameObject, pushPull.PulledObjectClient.gameObject } : new[] { gameObject };
			return MatrixManager.IsFloatingAt(context, Vector3Int.RoundToInt(predictedState.WorldPosition), isServer: false);
		}
	}

	public float SpeedClient
	{
		get => predictedSpeedClient;
		set
		{
			Logger.LogTraceFormat( "{0}: setting PREDICTED speed {1}->{2}", Category.Movement, gameObject.name, SpeedClient, value );
			predictedSpeedClient = value < 0 ? 0 : value;
		}
	}

	/// <summary>
	/// Player's clientside predicted move speed, applied to predicted moves
	/// </summary>
	private float predictedSpeedClient;

	///Does server claim this client is floating rn?
	public bool isFloatingClient => playerState.Impulse != Vector2.zero;

	/// Does your client think you should be floating rn? (Regardless of what server thinks)
	private bool isPseudoFloatingClient => predictedState.Impulse != Vector2.zero;

	/// Measure to avoid lerping back and forth in a lagspike
	/// where player simulated entire spacewalk (start and stop) without getting server's answer yet
	private bool blockClientMovement = false;

	private bool MoveCooldown = false; //cooldown is here just for client performance
	private void DoAction()
	{
		PlayerAction action = playerMove.SendAction();
		if (action.moveActions.Length != 0 && !MoveCooldown)
		{
			StartCoroutine(DoProcess(action));
		}
	}

	public bool DoAction(PlayerAction action)
	{
		if (action.moveActions.Length != 0 && !MoveCooldown)
		{
			StartCoroutine(DoProcess(action));
			return true;
		}
		return false;
	}

	private IEnumerator DoProcess(PlayerAction action)
	{
		MoveCooldown = true;
		//experiment: not enqueueing or processing action if floating.
		//arguably it shouldn't really be like that in the future
		if (!blockClientMovement && (!isPseudoFloatingClient && !isFloatingClient || playerScript.IsGhost))
		{
			Logger.LogTraceFormat( "Requesting {0} ({1} in queue)\nclientState = {2}\npredictedState = {3}", Category.Movement, 
				action.Direction(), pendingActions.Count, ClientState, predictedState );

			//experiment: not enqueueing or processing action if floating, unless we are stopped.
			//arguably it shouldn't really be like that in the future
			bool isGrounded = !MatrixManager.IsNonStickyAt(Vector3Int.RoundToInt(predictedState.WorldPosition), isServer: false);
			if ((isGrounded || playerScript.IsGhost || !IsMovingClient) && playerState.Active)
			{
				//RequestMoveMessage.Send(action);
				BumpType clientBump = CheckSlideAndBump(predictedState, false, ref action);

				action.isRun = UIManager.WalkRun.running;

				if (clientBump == BumpType.None || playerScript.IsGhost)
				{
					//move freely
					pendingActions.Enqueue(action);

					LastDirection = action.Direction();
					UpdatePredictedState();
				}
				else if (clientBump == BumpType.HelpIntent)
				{
					if (!isServer)
					{
						//only client performs this check, otherwise it would be performed twice by server
						CheckAndDoSwap(((Vector2)predictedState.WorldPosition + action.Direction()).RoundToInt(),
							inDirection: action.Direction() * -1,
							isServer: false);
					}

					//move freely
					pendingActions.Enqueue(action);
					LastDirection = action.Direction();
					UpdatePredictedState();
				}
				else
				{
					//cannot move -> tell server we're just bumping in that direction
					action.isBump = true;
					if (pendingActions == null || pendingActions.Count == 0)
					{
						PredictiveBumpInteract(Vector3Int.RoundToInt((Vector2)predictedState.WorldPosition + action.Direction()), action.Direction());
					}
					if (PlayerManager.LocalPlayer == gameObject)
					{
						playerSprites.ChangeAndSyncPlayerDirection(Orientation.From(action.Direction()));
						isBumping = true;
					}
					//cooldown is longer when humping walls or pushables
					//					yield return YieldHelper.DeciSecond;
					//					yield return YieldHelper.DeciSecond;
				}
			}
			else
			{
				action.isNonPredictive = true;
			}
			//Sending action for server approval
			CmdProcessAction(action);
		}
		else
		{
			Logger.LogTraceFormat( "Can't enqueue move: block = {0}, pseudoFloating = {1}, floating = {2}\nclientState = {3}\npredictedState = {4}"
				, Category.Movement, blockClientMovement, isPseudoFloatingClient, isFloatingClient, ClientState, predictedState );
		}

		yield return YieldHelper.DeciSecond;
		MoveCooldown = false;
		isBumping = false;
	}


	/// Predictive interaction with object you can't move through
	/// <param name="worldTile">Tile you're interacting with</param>
	/// <param name="direction">Direction you're pushing</param>
	private void PredictiveBumpInteract(Vector3Int worldTile, Vector2Int direction)
	{
		if ( playerScript.canNotInteract() )
		{
			return;
		}
		// Is the object pushable (iterate through all of the objects at the position):
		var pushPulls = MatrixManager.GetAt<PushPull>(worldTile, false);
		for (int i = 0; i < pushPulls.Count; i++)
		{
			var pushPull = pushPulls[i];
			if (pushPull && pushPull.gameObject != gameObject && pushPull.IsSolidClient)
			{
				//					Logger.LogTraceFormat( "Predictive pushing {0} from {1} to {2}", Category.PushPull, pushPulls[i].gameObject, worldTile, (Vector2)(Vector3)worldTile+(Vector2)direction );
				if (pushPull.TryPredictivePush(worldTile, direction))
				{
					//telling server what we just predictively pushed this thing
					//so that server could rollback it for client if it was wrong
					//instead of leaving it messed up permanently on client side
					CmdValidatePush(pushPull.gameObject);
				}
				break;
			}
		}
	}
	//Predictively pushing this player to target pos
	public bool PredictivePush(Vector2Int target, float speed = Single.NaN, bool followMode = false)
	{
		if (Matrix == null)
		{
			return false;
		}

		Vector3Int currentPos = ClientPosition;

		Vector3Int target3int = target.To3Int();

		if (!followMode && !MatrixManager.IsPassableAt(target3int, target3int, false)) //might have issues with windoors
		{
			return false;
		}

		Vector2Int direction = target - currentPos.To2Int();
		if (followMode)
		{
			SendMessage("FaceDirection", Orientation.From(direction), SendMessageOptions.DontRequireReceiver);
		}

		Logger.LogTraceFormat("Client predictive push to {0}", Category.PushPull, target);

		predictedState.MatrixId = MatrixManager.AtPoint(target3int, false).Id;
		predictedState.WorldPosition = target.To3Int();
		if ( !float.IsNaN( speed ) && speed > 0 ) {
			predictedState.Speed = speed;
		} else {
			predictedState.Speed = PushPull.DEFAULT_PUSH_SPEED;
		}

		OnClientStartMove().Invoke(currentPos, target3int); //?

		if (!isServer)
		{
			//				//Lerp if not server.
			//				//for some reason player pulling prediction doesn't have 1 frame delay on server
			//				//while everything else does.
			Lerp();
		}

		LastDirection = direction;
		return true;
	}


	private void UpdatePredictedState()
	{
		if (!isLocalPlayer)
		{
			return;
		}
		if (pendingActions.Count == 0)
		{
			//plain assignment if there's nothing to predict
			predictedState = playerState;
		}
		else
		{
			//redraw prediction point from received serverState using pending actions
			PlayerState tempState = playerState;
			var state = predictedState;
			int curPredictedMove = state.MoveNumber;

			foreach (PlayerAction action in pendingActions)
			{
				//isReplay determines if this action is a replayed action for use in the prediction system
				bool isReplay = state.MoveNumber < curPredictedMove;
				var nextState = NextStateClient(tempState, action, isReplay);

				tempState = nextState;

				//					Logger.LogTraceFormat("Client generated {0}", Category.Movement, tempState);
			}
			var newPos = tempState.WorldPosition;
			var oldPos = state.WorldPosition;

			LastDirection = Vector2Int.RoundToInt(newPos - oldPos);

			if (LastDirection != Vector2.zero)
			{
				OnClientStartMove().Invoke(oldPos.RoundToInt(), newPos.RoundToInt());
			}


			predictedState = tempState;

		}
	}

	private PlayerState NextStateClient(PlayerState state, PlayerAction action, bool isReplay)
	{
		if ( !playerScript.IsGhost )
		{
			if ( !playerScript.playerHealth.IsSoftCrit )
			{
				SpeedClient = action.isRun ? playerMove.RunSpeed : playerMove.WalkSpeed;
			}
			else
			{
				SpeedClient = playerMove.CrawlSpeed;
			}
		}

		var nextState = NextState(state, action, isReplay);

		nextState.Speed = SpeedClient;

		return nextState;
	}

	/// Called when PlayerMoveMessage is received
	public void UpdateClientState(PlayerState newState)
	{
		var newWorldPos = Vector3Int.RoundToInt(newState.WorldPosition);
		OnUpdateRecieved().Invoke(newWorldPos);

		playerState = newState;

		//			if ( !isServer )
		//			{
		//				Logger.LogTraceFormat( "Got server update {0}", Category.Movement, newState );
		//			}

		//Ignore "Follow Updates" if you're pulling it
		if (newState.Active
			 && pushPull != null && pushPull.IsPulledByClient(PlayerManager.LocalPlayerScript?.pushPull)
		)
		{
			return;
		}

		if (!isLocalPlayer)
		{
			predictedState = newState;
		}

		if (playerState.MatrixId != predictedState.MatrixId && isLocalPlayer)
		{
			PlayerState crossMatrixState = predictedState;
			crossMatrixState.MatrixId = playerState.MatrixId;
			crossMatrixState.WorldPosition = predictedState.WorldPosition;
			crossMatrixState.Impulse = playerState.Impulse;
			predictedState = crossMatrixState;
		}

		if (blockClientMovement)
		{
			if (isFloatingClient)
			{
				Logger.Log($"Spacewalk approved. Got {playerState}\nPredicting {predictedState}", Category.Movement);
				ClearQueueClient();
				blockClientMovement = false;
			}
			else
			{
				Logger.LogWarning("Movement blocked. Waiting for a sign of approval for experienced flight", Category.Movement);
				return;
			}
		}
		if (isFloatingClient)
		{
			LastDirection = playerState.Impulse;
		}

		//don't reset predicted state if it guessed impulse correctly
		//or server is just approving old moves when you weren't flying yet
		if (isFloatingClient || isPseudoFloatingClient)
		{
			//rollback prediction if either wrong impulse on given step OR both impulses are non-zero and point in different directions
			bool spacewalkReset = predictedState.Impulse != playerState.Impulse
							 && ((predictedState.MoveNumber == playerState.MoveNumber && !pushPull.IsPullingSomethingClient)
								  || playerState.Impulse != Vector2.zero && predictedState.Impulse != Vector2.zero);
			bool wrongFloatDir = playerState.MoveNumber < predictedState.MoveNumber &&
							playerState.Impulse != Vector2.zero &&
							playerState.Impulse.normalized != (Vector2)(predictedState.Position - playerState.Position).normalized;
			if (spacewalkReset || wrongFloatDir)
			{
				Logger.LogWarning($"{nameof(spacewalkReset)}={spacewalkReset}, {nameof(wrongFloatDir)}={wrongFloatDir}", Category.Movement);
				ClearQueueClient();
				RollbackPrediction();

				OnClientStartMove().Invoke( newWorldPos-LastDirection.RoundToInt(), newWorldPos );
			}
			return;
		}
		if (pendingActions != null)
		{
			//invalidate queue if serverstate was never predicted
			bool serverAhead = playerState.MoveNumber > predictedState.MoveNumber;
			bool posMismatch = playerState.MoveNumber == predictedState.MoveNumber
							   && playerState.Position != predictedState.Position;
			bool wrongMatrix = playerState.MatrixId != predictedState.MatrixId && playerState.MoveNumber == predictedState.MoveNumber;
			if (serverAhead || posMismatch || wrongMatrix)
			{
				Logger.LogWarning($"{nameof(serverAhead)}={serverAhead}, {nameof(posMismatch)}={posMismatch}, {nameof(wrongMatrix)}={wrongMatrix}", Category.Movement);
				ClearQueueClient();
				RollbackPrediction();
			}
			else
			{
				//removing actions already acknowledged by server from pending queue
				while (pendingActions.Count > 0 &&
						pendingActions.Count > predictedState.MoveNumber - playerState.MoveNumber)
				{
					pendingActions.Dequeue();
				}
			}
			UpdatePredictedState();
		}
	}
	/// Reset client predictedState to last received server state (a.k.a. playerState)
	public void RollbackPrediction()
	{
		//			Logger.Log( $"Rollback {predictedState}\n" +
		//			           $"To       {playerState}" );
		predictedState = playerState;
		StartCoroutine(RollbackPullables()); //? verify if robust
	}

	private IEnumerator RollbackPullables()
	{
		yield return YieldHelper.EndOfFrame;
		if (gameObject == PlayerManager.LocalPlayer
			 && pushPull && pushPull.IsPullingSomethingClient)
		{
			//Rollback whatever you're pulling predictively, too
			pushPull.PulledObjectClient.Pushable.RollbackPrediction();
		}
	}

	/// Clears client pending actions queue
	public void ClearQueueClient()
	{
		//			Logger.Log("Resetting queue as requested by server!");
		if (pendingActions != null && pendingActions.Count > 0)
		{
			pendingActions.Clear();
		}
	}

	/// Ignore further predictive movement until approval message is received
	/// (Or wait time is up, then prediction is rolled back)
	private IEnumerator BlockMovement()
	{
		blockClientMovement = true;
		yield return new WaitForSeconds(2f);
		if (blockClientMovement)
		{
			Logger.LogWarning("Looks like you got stuck. Rolling back predictive moves", Category.Movement);
			RollbackPrediction();
		}
		blockClientMovement = false;
	}

	///Lerping; simulating space walk by server's orders or initiate/stop them on client
	///Using predictedState for your own player and playerState for others
	private void CheckMovementClient()
	{
		playerState.NoLerp = false;

		//Space walk checks
		if (!IsWeightlessClient)
		{
			if (isPseudoFloatingClient)
			{
				//Logger.Log( "Stopped clientside floating to avoid going through walls" );

				//Zeroing lastDirection after hitting an obstacle
				LastDirection = Vector2.zero;

				//stop floating on client (if server isn't responding in time) to avoid players going through walls
				predictedState.Impulse = Vector2.zero;
				//Stopping spacewalk increases move number
				predictedState.MoveNumber++;

				if (!isFloatingClient && playerState.MoveNumber < predictedState.MoveNumber)
				{
					Logger.Log($"Finished unapproved flight, blocking. predictedState:\n{predictedState}", Category.Movement);
					//Client figured out that he just finished spacewalking
					//and server is yet to approve the fact that it even started.
					StartCoroutine(BlockMovement());
				}
			}
		}
		else
		{
			if (predictedState.Impulse == Vector2.zero && LastDirection != Vector2.zero)
			{
				if (pendingActions == null || pendingActions.Count == 0)
				{
					//						not initiating predictive spacewalk without queued actions
					LastDirection = Vector2.zero;
					return;
				}
				//client initiated space dive.
				predictedState.Impulse = LastDirection;
				Logger.Log($"Client init floating with impulse {LastDirection}. FC={isFloatingClient},PFC={isPseudoFloatingClient}", Category.Movement);
			}

			//Perpetual floating sim
			if (ClientPositionReady && isPseudoFloatingClient)
			{
				var oldPos = predictedState.WorldPosition;

				//Extending prediction by one tile if player's transform reaches previously set goal
				Vector3Int newGoal = Vector3Int.RoundToInt(predictedState.Position + (Vector3)predictedState.Impulse);
				predictedState.Position = newGoal;

				var newPos = predictedState.WorldPosition;

				OnClientStartMove().Invoke(oldPos.RoundToInt(), newPos.RoundToInt());
			}
		}
	}

	private void Lerp()
	{
		if (!ClientPositionReady)
		{
			//PlayerLerp
			var worldPos = predictedState.WorldPosition;
			Vector3 targetPos = MatrixManager.WorldToLocal(worldPos, MatrixManager.Get(Matrix));

			if (playerState.NoLerp || Vector3.Distance(transform.localPosition, targetPos) > 30)
			{
				transform.localPosition = targetPos;
			}
			else
			{
				transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos,
				predictedState.Speed * Time.deltaTime * transform.localPosition.SpeedTo(targetPos));
			}

			if (ClientPositionReady)
			{
				OnClientTileReached().Invoke(Vector3Int.RoundToInt(worldPos));
				// Check for swap once movement is done, to prevent us and another player moving into the same tile
				if (!isServer && !playerScript.IsGhost)
				{
					//only check on client otherwise server would check this twice
					CheckAndDoSwap(worldPos.RoundToInt(), lastDirection*-1, isServer: false);
				}

			}
		}
	}
}
