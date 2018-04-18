using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
	public partial class PlayerSync
	{
		//Server-only fields, don't concern clients in any way
		private PlayerState serverTargetState;

		private PlayerState serverState;
		private Queue<PlayerAction> serverPendingActions;
		[SyncVar] [Obsolete] private PlayerState serverStateCache; //todo: phase it out, it actually concerns clients

		/// Max size of serverside queue, client will be rolled back and punished if it overflows
		private readonly int maxServerQueue = 10;

		/// Amount of soft punishments before the hard one kicks in
		private readonly int maxWarnings = 3;

		private int playerWarnings;
		private Vector2 serverLastDirection;

		//TODO: Remove the space damage coroutine when atmos is implemented
		private bool isApplyingSpaceDmg;

		private bool consideredFloatingServer => serverState.Impulse != Vector2.zero;

		/// idk if it's robust enough, but it seems to work
		private bool ServerPositionsMatch => serverTargetState.WorldPosition == serverState.WorldPosition;

		private bool MatrixSwitchAhead => serverTargetState.MatrixId != serverState.MatrixId; //fixme: wonky

		public override void OnStartServer() {
			PullObjectID = NetworkInstanceId.Invalid;
			base.OnStartServer();
			InitServerState();
		}

		[Server]
		private void InitServerState() {
			Vector3Int worldPos = Vector3Int.RoundToInt( (Vector2) transform.position ); //cutting off Z-axis & rounding
			MatrixInfo matrixAtPoint = MatrixManager.Instance.AtPoint( worldPos );
			PlayerState state = new PlayerState {
				MoveNumber = 0,
				MatrixId = matrixAtPoint.Id,
				WorldPosition = worldPos
			};
			Debug.Log( $"{PlayerList.Instance.Get( gameObject ).Name}: InitServerState for {worldPos} found matrix {matrixAtPoint} resulting in\n{state}" );
			serverState = state;
			serverStateCache = state;
			serverTargetState = state;
		}

		[Command( channel = 0 )]
		private void CmdProcessAction( PlayerAction action ) {
			//add action to server simulation queue
			serverPendingActions.Enqueue( action );

			//Do not cache the position if the player is a ghost
			//or else new players will sync the deadbody with the last pos
			//of the gost:
			if ( playerMove.isGhost ) {
				return;
			}
			//Rollback pos and punish player if server queue size is more than max size
			if ( serverPendingActions.Count > maxServerQueue ) {
				RollbackPosition();
				if ( ++playerWarnings < maxWarnings ) {
					TortureChamber.Torture( playerScript, TortureSeverity.S );
				} else {
					TortureChamber.Torture( playerScript, TortureSeverity.L );
				}
				return;
			}
				
			serverStateCache = serverState;
		}

		/// Push player in direction.
		/// Impulse should be consumed after one tile if indoors,
		/// and last indefinitely (until hit by obstacle) if you pushed someone into deep space 
		[Server]
		public void Push( Vector2Int direction ) {
			if ( direction == Vector2Int.zero ) {
				Debug.Log( "Push with zero impulse??" );
				return;
			}
			serverState.Impulse = direction;
			serverTargetState.Impulse = direction;
			if ( matrix != null ) {
				Vector3Int pushGoal =
					Vector3Int.RoundToInt( serverState.Position + (Vector3) serverTargetState.Impulse );
				if ( matrix.IsPassableAt( pushGoal ) ) {
					Debug.Log( $"Server push to {pushGoal}" );
					serverTargetState.Position = pushGoal;
					serverTargetState.ImportantFlightUpdate = true;
					serverTargetState.ResetClientQueue = true;
				} else {
					serverState.Impulse = Vector2.zero;
					serverTargetState.Impulse = Vector2.zero;
				}
			}
		}

		/// Manually set player to a specific world position.
		/// Also clears prediction queues.
		/// <param name="worldPos">The new position to "teleport" player</param>
		[Server]
		public void SetPosition( Vector3 worldPos ) {
			ClearQueueServer();
			Vector3Int roundedPos = Vector3Int.RoundToInt( worldPos );
			MatrixInfo newMatrix = MatrixManager.Instance.AtPoint( roundedPos );
			//Note the client queue reset
			var newState = new PlayerState {
				MoveNumber = 0,
				MatrixId = newMatrix.Id,
				WorldPosition = roundedPos,
				ResetClientQueue = true
			};
			serverState = newState;
			serverTargetState = newState;
			serverStateCache = newState;
			SyncMatrix();
			NotifyPlayers();
		}

		///	When lerp is finished, inform players of new state  
		[Server]
		private void TryNotifyPlayers() {
			if ( ServerPositionsMatch ) {
//				When serverState reaches its planned destination,
//				embrace all other updates like updated moveNumber and flags
				serverState = serverTargetState;
//				if ( MatrixSwitchAhead ) {
//					Debug.Log( "Hey, it's worth syncing matrix!" );
					SyncMatrix();
//				}
				NotifyPlayers();
			}
		}

		[Server]
		private void SyncMatrix() {
			registerTile.ParentNetId = MatrixManager.Instance.Get( serverState.MatrixId ).NetId;
		}

		[Server]
		public void NotifyPlayer( GameObject recipient ) {
			PlayerMoveMessage.Send( recipient, gameObject, serverState );
		}

		[Server]
		public void NotifyPlayers() {
			//Do not cache the position if the player is a ghost
			//or else new players will sync the deadbody with the last pos
			//of the ghost:
			if ( !playerMove.isGhost ) {
				serverStateCache = serverState;
			}
			//Generally not sending mid-flight updates (unless there's a sudden change of course etc.)
			if ( !serverState.ImportantFlightUpdate && consideredFloatingServer ) {
				return;
			}

			PlayerMoveMessage.SendToAll( gameObject, serverState );
			ClearStateFlags();
		}

		/// Clears server pending actions queue
		private void ClearQueueServer() {
//			Debug.Log("Server queue wiped!");
			if ( serverPendingActions != null && serverPendingActions.Count > 0 ) {
				serverPendingActions.Clear();
			}
		}


		/// Clear all queues and
		/// inform players of true serverState
		[Server]
		private void RollbackPosition() {
			SetPosition( serverState.Position );
		}

		/// try getting moves from server queue if server and target states match
		[Server]
		private void CheckTargetUpdate() {
			//checking only player movement for now
			if ( ServerPositionsMatch ) {
				TryUpdateServerTarget();
			}
		}

		//Currently used to set the pos of a player that has just been dragged by another player
		//Fixme: prone to exploits
		[Command]
		public void CmdSetPositionFromReset( GameObject fromObj, GameObject otherPlayer, Vector3 setPos ) {
			if ( fromObj.GetComponent<IPlayerSync>() == null ) //Validation
			{
				return;
			}
			IPlayerSync otherPlayerSync = otherPlayer.GetComponent<IPlayerSync>();
			otherPlayerSync.SetPosition( setPos );
		}

		/// Tries to assign next target from queue to serverTargetState if there are any
		/// (In order to start lerping towards it)
		[Server]
		private void TryUpdateServerTarget() {
			if ( serverPendingActions.Count == 0 ) {
				return;
			}

			var nextAction = serverPendingActions.Peek();
			if ( !IsPointlessMove( serverTargetState, nextAction ) ) {
				if ( consideredFloatingServer ) {
					Debug.LogWarning( "Server ignored move while player is floating" );
					serverPendingActions.Dequeue();
					return;
				}
				PlayerState nextState = NextStateServer( serverTargetState, serverPendingActions.Dequeue() );
				serverLastDirection = Vector2Int.RoundToInt( nextState.WorldPosition - serverTargetState.WorldPosition );
				serverTargetState = nextState;
//				Debug.Log($"Server Updated target {serverTargetState}. {serverPendingActions.Count} pending");
			} else {
				Debug.LogWarning(
					$"Pointless move {serverTargetState}+{nextAction.keyCodes[0]} Rolling back to {serverState}" );
				RollbackPosition();
			}
		}
		/// NextState that also subscribes player to matrix rotations 
		[Server]
		private PlayerState NextStateServer( PlayerState state, PlayerAction action ) {
			bool matrixChangeDetected;
			PlayerState nextState = NextState( state, action, out matrixChangeDetected );

			if ( !matrixChangeDetected ) {
				return nextState;
			}
			var newMatrix = MatrixManager.Instance.Get( nextState.MatrixId );
			Debug.Log( $"Matrix will change to {newMatrix}" );
			if ( newMatrix.MatrixMove ) {
				//Subbing to new matrix rotations
				newMatrix.MatrixMove.onRotation += OnRotation;
				Debug.Log( $"Registered rotation listener to {newMatrix.MatrixMove}" );
			}
			//Unsubbing from old matrix rotations
			MatrixMove oldMatrixMove = MatrixManager.Instance.Get( matrix ).MatrixMove;
			if ( oldMatrixMove ) {
				Debug.Log( $"Unregistered rotation listener from {oldMatrixMove}" );
				oldMatrixMove.onRotation -= OnRotation;
			}
			return nextState;
		}
		
		[Server]
		private void OnRotation(Orientation from, Orientation to) {
			playerSprites.ChangePlayerDirection( Orientation.DegreeBetween( from, to ) );
		}

		/// Lerping and ensuring server authority for space walk
		[Server]
		private void CheckMovementServer() { 
			if ( !ServerPositionsMatch ) {
				//Lerp on server if it's worth lerping 
				//and inform players if serverState reached targetState afterwards 
				serverState.WorldPosition =
					Vector3.MoveTowards( serverState.WorldPosition, serverTargetState.WorldPosition, playerMove.speed * Time.deltaTime );
				TryNotifyPlayers();
			}
			//Space walk checks
			bool isFloating = MatrixManager.Instance.IsFloatingAt( Vector3Int.RoundToInt(serverTargetState.WorldPosition) );

			if ( isFloating ) {
				if ( serverTargetState.Impulse == Vector2.zero && serverLastDirection != Vector2.zero ) {
					//server initiated space dive. 						
					serverTargetState.Impulse = serverLastDirection;
					serverTargetState.ImportantFlightUpdate = true;
					serverTargetState.ResetClientQueue = true;
				}
				//Perpetual floating sim
				if ( ServerPositionsMatch && !serverTargetState.ImportantFlightUpdate ) {
					//Extending prediction by one tile if player's transform reaches previously set goal
					Vector3Int newGoal = Vector3Int.RoundToInt( serverTargetState.Position + (Vector3) serverTargetState.Impulse );
					serverTargetState.Position = newGoal;
					ClearQueueServer();
				}
			}
			if ( consideredFloatingServer && !isFloating ) {
				//finish floating. players will be notified as soon as serverState catches up
				serverState.Impulse = Vector2.zero;
				serverTargetState.Impulse = Vector2.zero;
				serverTargetState.ResetClientQueue = true;
				//Stopping spacewalk increases move number
				serverTargetState.MoveNumber++;
				//removing lastDirection when we hit an obstacle in space
				serverLastDirection = Vector2.zero;
			}
			CheckSpaceDamage();
		}

		/// Checking whether player should suffocate
		[Server]
		private void CheckSpaceDamage() {
			if ( matrix.IsSpaceAt( Vector3Int.RoundToInt( serverState.Position ) )
			     && !healthBehaviorScript.IsDead && !isApplyingSpaceDmg ) {
//				Hurting people in space even if they are next to the wall
				StartCoroutine( ApplyTempSpaceDamage() );
				isApplyingSpaceDmg = true;
			}
		}

		//TODO: Remove this when atmos is implemented 
		///This prevents players drifting into space indefinitely 
		private IEnumerator ApplyTempSpaceDamage() {
			yield return new WaitForSeconds( 1f );
			healthBehaviorScript.ApplyDamage( null, 5, DamageType.OXY, BodyPartType.HEAD );
			isApplyingSpaceDmg = false;
		}
	}
}