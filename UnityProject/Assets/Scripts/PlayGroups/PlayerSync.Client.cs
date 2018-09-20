using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class PlayerSync
	{
		//Client-only fields, don't concern server
		private Vector3IntEvent onClientTileReached = new Vector3IntEvent();
		public Vector3IntEvent OnClientTileReached() {
			return onClientTileReached;
		}

		/// Trusted state, received from server
		private PlayerState playerState;

		/// Client predicted state
		private PlayerState predictedState;

		private Queue<PlayerAction> pendingActions;
		private Vector2 lastDirection;

		/// Last move direction, used for space walking simulation
		private Vector2 LastDirection {
			get { return lastDirection; }
			set {
//				if ( value == Vector2.zero ) {
//					Logger.Log( $"Setting client LastDirection to {value}!" );
//				}
				lastDirection = value;
			}
		}
		/// Does client's transform pos match state pos? Ignores Z-axis.
		private bool ClientPositionReady {
			get {
				var state = isLocalPlayer ? predictedState : playerState;
				return ( Vector2 ) state.Position == ( Vector2 ) transform.localPosition;
			}
		}
		/// Does ghosts's transform pos match state pos? Ignores Z-axis.
		private bool GhostPositionReady {
			get {
				var state = isLocalPlayer ? predictedState : playerState;
				return ( Vector2 ) state.WorldPosition == ( Vector2 ) playerScript.ghost.transform.position;
			}
		}

		///Does server claim this client is floating rn?
		private bool isFloatingClient => playerState.Impulse != Vector2.zero;

		/// Does your client think you should be floating rn? (Regardless of what server thinks)
		private bool isPseudoFloatingClient => predictedState.Impulse != Vector2.zero;

		/// Measure to avoid lerping back and forth in a lagspike
		/// where player simulated entire spacewalk (start and stop) without getting server's answer yet
		private bool blockClientMovement = false;

//		public override void OnStartClient() {
//			StartCoroutine( WaitForLoad() );
//			base.OnStartClient();
//		}
		private bool MoveCooldown = false; //cooldown is here just for client performance
		private void DoAction() {
			PlayerAction action = playerMove.SendAction();
			if ( action.keyCodes.Length != 0  && !MoveCooldown ) {
				StartCoroutine( DoProcess( action ) );
			}
		}

		private IEnumerator DoProcess( PlayerAction action ) {
			MoveCooldown = true;
			//experiment: not enqueueing or processing action if floating.
			//arguably it shouldn't really be like that in the future
			if ( !isPseudoFloatingClient && !isFloatingClient && !blockClientMovement ) {
//				Logger.Log($"{gameObject.name} requesting {action.Direction()} ({pendingActions.Count} in queue)");
				if ( CanMoveThere( predictedState, action ) ) {
					pendingActions.Enqueue( action );

					LastDirection = action.Direction();
//					UpdatePredictedState();
				} //else {
//					PredictiveInteract( Vector3Int.RoundToInt( (Vector2)predictedState.WorldPosition + action.Direction() ), action.Direction());
//				}

				UpdatePredictedState();

				//Seems like Cmds are reliable enough in this case
				CmdProcessAction( action );
				//				RequestMoveMessage.Send(action);
			}

			yield return YieldHelper.DeciSecond;
			MoveCooldown = false;
		}

		/// Predictive interaction with object you can't move through
		/// <param name="worldTile">Tile you're interacting with</param>
		/// <param name="direction">Direction you're pushing</param>
		private void PredictiveInteract( Vector3Int worldTile, Vector2Int direction ) {
			// Is the object pushable (iterate through all of the objects at the position):
			PushPull[] pushPulls = MatrixManager.GetAt<PushPull>( worldTile ).ToArray();
			for ( int i = 0; i < pushPulls.Length; i++ ) {
				var pushPull = pushPulls[i];
				if ( pushPull && pushPull.gameObject != gameObject && pushPull.CanBePushed ) {
//					Logger.LogTraceFormat( "Predictive pushing {0} from {1} to {2}", Category.PushPull, pushPulls[i].gameObject, worldTile, (Vector2)(Vector3)worldTile+(Vector2)direction );
					pushPull.TryPredictivePush( worldTile, direction );
					break;
				}
			}
		}
		//Predictively pushing this player
		public void PredictivePush(Vector2Int direction)//todo: untested!
		{
			if (direction == Vector2Int.zero)
			{
				Logger.Log("PredictivePush with zero impulse??", Category.PushPull);
				return;
			}

			predictedState.Impulse = direction;
			if (matrix != null)
			{
				Vector3Int pushGoal =
					Vector3Int.RoundToInt(playerState.Position + (Vector3)predictedState.Impulse);
				if (matrix.IsPassableAt(pushGoal))
				{
					Logger.Log($"Client predictive push to {pushGoal}", Category.PushPull);
					predictedState.Position = pushGoal;
					predictedState.ImportantFlightUpdate = true;
					predictedState.ResetClientQueue = true;
				}
				else
				{
					predictedState.Impulse = Vector2.zero;
				}
			}
		}

		private void UpdatePredictedState() {
			if ( pendingActions.Count == 0 ) {
				//plain assignment if there's nothing to predict
				RollbackPrediction();
			} else {
				//redraw prediction point from received serverState using pending actions
				PlayerState tempState = playerState;
				int curPredictedMove = predictedState.MoveNumber;

				foreach ( PlayerAction action in pendingActions ) {
					//isReplay determines if this action is a replayed action for use in the prediction system
					bool isReplay = predictedState.MoveNumber <= curPredictedMove;
//					bool matrixChanged;
					tempState = NextStateClient( tempState, action, /*out matrixChanged,*/ isReplay );
//					if ( tempState.WorldPosition == playerState.WorldPosition ) { //?
//
//					}

//					if ( matrixChanged ) {
//						Logger.Log( $"{gameObject.name}: Predictive matrix change to {tempState}, {pendingActions.Count} pending" );
//					}
//					Logger.Log($"Generated {tempState}");
				}
				predictedState = tempState;
			}
		}

		private PlayerState NextStateClient( PlayerState state, PlayerAction action,/* out bool matrixChanged,*/ bool isReplay ) {
			if ( !CanMoveThere( state, action ) ) {
				//gotta try pushing things
				PredictiveInteract( Vector3Int.RoundToInt( (Vector2)predictedState.WorldPosition + action.Direction() ), action.Direction());
				return state;
			}
			bool matrixChanged;
			return NextState( state, action, out matrixChanged, isReplay );
		}

		/// Called when PlayerMoveMessage is received
		public void UpdateClientState( PlayerState state ) {
			onUpdateReceived.Invoke( Vector3Int.RoundToInt( state.WorldPosition ) );

			playerState = state;
//			if ( !isServer ) {
//				Logger.Log( $"Got server update {playerState}" );
//			}

			if ( playerState.MatrixId != predictedState.MatrixId && isLocalPlayer ) {
				PlayerState crossMatrixState = predictedState;
				crossMatrixState.MatrixId = playerState.MatrixId;
				crossMatrixState.WorldPosition = predictedState.WorldPosition;
				crossMatrixState.Impulse = playerState.Impulse;
				predictedState = crossMatrixState;
			}

			if ( blockClientMovement ) {
				if ( isFloatingClient ) {
					Logger.Log( $"Spacewalk approved. Got {playerState}\nPredicting {predictedState}",Category.Movement );
					ClearQueueClient();
					blockClientMovement = false;
				} else {
					Logger.LogWarning( "Movement blocked. Waiting for a sign of approval for experienced flight",Category.Movement );
					return;
				}
			}
			if ( isFloatingClient ) {
				LastDirection = playerState.Impulse;
			}

			//don't reset predicted state if it guessed impulse correctly
			//or server is just approving old moves when you weren't flying yet
			if ( isFloatingClient || isPseudoFloatingClient ) {
				//rollback prediction if either wrong impulse on given step OR both impulses are non-zero and point in different directions
				bool spacewalkReset = predictedState.Impulse != playerState.Impulse
				                 && ( predictedState.MoveNumber == playerState.MoveNumber
				                      || playerState.Impulse != Vector2.zero && predictedState.Impulse != Vector2.zero );
				bool wrongFloatDir = playerState.MoveNumber < predictedState.MoveNumber &&
				                playerState.Impulse != Vector2.zero &&
				                playerState.Impulse.normalized != (Vector2)(predictedState.Position  - playerState.Position).normalized;
				if ( spacewalkReset || wrongFloatDir ) {
					Logger.LogWarning( $"{nameof(spacewalkReset)}={spacewalkReset}, {nameof(wrongFloatDir)}={wrongFloatDir}",Category.Movement );
					ClearQueueClient();
					RollbackPrediction();
				}
				return;
			}
			if ( pendingActions != null ) {
				//invalidate queue if serverstate was never predicted
				bool serverAhead = playerState.MoveNumber > predictedState.MoveNumber;
				bool posMismatch = playerState.MoveNumber == predictedState.MoveNumber
				                   && playerState.Position != predictedState.Position;
				bool wrongMatrix = playerState.MatrixId != predictedState.MatrixId && playerState.MoveNumber == predictedState.MoveNumber;
				if ( serverAhead || posMismatch || wrongMatrix ) {
					Logger.LogWarning( $"{nameof(serverAhead)}={serverAhead}, {nameof(posMismatch)}={posMismatch}, {nameof(wrongMatrix)}={wrongMatrix}",Category.Movement);
					ClearQueueClient();
					RollbackPrediction();
				} else {
					//removing actions already acknowledged by server from pending queue
					while ( pendingActions.Count > 0 &&
					        pendingActions.Count > predictedState.MoveNumber - playerState.MoveNumber ) {
						pendingActions.Dequeue();
					}
				}
				UpdatePredictedState();
			}
		}
		/// Reset client predictedState to last received server state (a.k.a. playerState)
		public void RollbackPrediction() {
//			Logger.Log( $"Rollback {predictedState}\n" +
//			           $"To       {playerState}" );
			predictedState = playerState;
		}

		/// Clears client pending actions queue
		public void ClearQueueClient() {
//			Logger.Log("Resetting queue as requested by server!");
			if ( pendingActions != null && pendingActions.Count > 0 ) {
				pendingActions.Clear();
			}
		}

		/// Ignore further predictive movement until approval message is received
		/// (Or wait time is up, then prediction is rolled back)
		private IEnumerator BlockMovement() {
			blockClientMovement = true;
			yield return new WaitForSeconds(2f);
			if ( blockClientMovement ) {
				Logger.LogWarning( "Looks like you got stuck. Rolling back predictive moves",Category.Movement);
				RollbackPrediction();
			}
			blockClientMovement = false;
		}

		///Lerping; simulating space walk by server's orders or initiate/stop them on client
		///Using predictedState for your own player and playerState for others
		private void CheckMovementClient()
		{
			PlayerState state = isLocalPlayer ? predictedState : playerState;

			if ( !ClientPositionReady ) {
				//PlayerLerp
				Vector3 targetPos = MatrixManager.WorldToLocal(state.WorldPosition, MatrixManager.Get( matrix ) );
				transform.localPosition = Vector3.MoveTowards( transform.localPosition,
					targetPos,
					playerMove.speed * Time.deltaTime );
				//failsafe
				if ( playerState.NoLerp || Vector3.Distance( transform.localPosition, targetPos ) > 30 ) {
					transform.localPosition = targetPos;
				}
			}

			playerState.NoLerp = false;

			bool isFloating = MatrixManager.IsFloatingAt( Vector3Int.RoundToInt(state.WorldPosition) );
			//Space walk checks
			if ( isPseudoFloatingClient && !isFloating ) {
//                Logger.Log( "Stopped clientside floating to avoid going through walls" );

				//stop floating on client (if server isn't responding in time) to avoid players going through walls
				predictedState.Impulse = Vector2.zero;
				//Stopping spacewalk increases move number
				predictedState.MoveNumber++;
				//Zeroing lastDirection after hitting an obstacle
				LastDirection = Vector2.zero;

				if ( !isFloatingClient && playerState.MoveNumber < predictedState.MoveNumber ) {
					Logger.Log( $"Finished unapproved flight, blocking. predictedState:\n{predictedState}",Category.Movement );
					//Client figured out that he just finished spacewalking
					//and server is yet to approve the fact that it even started.
					StartCoroutine( BlockMovement() );
				}
			}
			if ( isFloating ) {
				if ( state.Impulse == Vector2.zero && LastDirection != Vector2.zero ) {
					if ( pendingActions == null || pendingActions.Count == 0 ) {
//						Logger.LogWarning( "Just saved your ass; not initiating predictive spacewalk without queued actions" );
						LastDirection = Vector2.zero;
						return;
					}
					//client initiated space dive.
					state.Impulse = LastDirection;
					if ( isLocalPlayer ) {
						predictedState.Impulse = state.Impulse;
					} else {
						playerState.Impulse = state.Impulse;
					}
					Logger.Log($"Client init floating with impulse {LastDirection}. FC={isFloatingClient},PFC={isPseudoFloatingClient}",Category.Movement);
				}

				//Perpetual floating sim
				if ( ClientPositionReady ) {
					//Extending prediction by one tile if player's transform reaches previously set goal
					Vector3Int newGoal = Vector3Int.RoundToInt( state.Position + (Vector3) state.Impulse );
					if ( !isLocalPlayer ) {
						playerState.Position = newGoal;
					}
					predictedState.Position = newGoal;
				}
			}
		}
	}
