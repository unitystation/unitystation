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

		/// Current server state. Lerps towards target state, so its position can be non-integer in the process.
		/// Updates to players, however, are only sent when it approaches target state's position, 
		/// therefore position is always sent as integer (at least in SendToAll()).
		private PlayerState serverState;

		/// Future/target server state. All future changes go here 
		/// and are applied to serverState when their positions match up 
		private PlayerState serverTargetState;

		private Queue<PlayerAction> serverPendingActions;

		/// Max size of serverside queue, client will be rolled back and punished if it overflows
		private readonly int maxServerQueue = 10;

		/// Amount of soft punishments before the hard one kicks in
		private readonly int maxWarnings = 3;

		private int playerWarnings;

		/// Last direction that player moved in. Currently works more like a true impulse, therefore is zero-able
		private Vector2 serverLastDirection;

		//TODO: Remove the space damage coroutine when atmos is implemented
		private bool isApplyingSpaceDmg;

		/// 
		public bool IsInSpace => MatrixManager.IsFloatingAt(Vector3Int.RoundToInt(serverTargetState.WorldPosition));
		
		/// Whether player is considered to be floating on server
		private bool consideredFloatingServer => serverState.Impulse != Vector2.zero;

		/// Do current and target server positions match?
		private bool ServerPositionsMatch => serverTargetState.WorldPosition == serverState.WorldPosition;

		public override void OnStartServer()
		{
			PullObjectID = NetworkInstanceId.Invalid;
			base.OnStartServer();
			InitServerState();
		}
//TODO: don't allow walking when stopped in vacuum
		/// 
		[Server]
		private void InitServerState()
		{
			Vector3Int worldPos = Vector3Int.RoundToInt((Vector2) transform.position); //cutting off Z-axis & rounding
			MatrixInfo matrixAtPoint = MatrixManager.AtPoint(worldPos);
			PlayerState state = new PlayerState
			{
				MoveNumber = 0,
				MatrixId = matrixAtPoint.Id,
				WorldPosition = worldPos
			};
//			Logger.Log( $"{PlayerList.Instance.Get( gameObject ).Name}: InitServerState for {worldPos} found matrix {matrixAtPoint} resulting in\n{state}" );
			serverState = state;
			serverTargetState = state;
			
			//Subbing to new matrix rotations
			if ( matrixAtPoint.MatrixMove != null ) {
				matrixAtPoint.MatrixMove.OnRotate.AddListener( OnRotation );
			}
		}

		[Command(channel = 0)]
		private void CmdProcessAction(PlayerAction action)
		{
			//add action to server simulation queue
			serverPendingActions.Enqueue(action);

			//Do not cache the position if the player is a ghost
			//or else new players will sync the deadbody with the last pos
			//of the gost:
			if (playerMove.isGhost)
			{
				return;
			}

			//Rollback pos and punish player if server queue size is more than max size
			if (serverPendingActions.Count > maxServerQueue)
			{
				RollbackPosition();
				if (++playerWarnings < maxWarnings)
				{
					TortureChamber.Torture(playerScript, TortureSeverity.S);
				}
				else
				{
					TortureChamber.Torture(playerScript, TortureSeverity.L);
				}
			}
		}

		/// Push player in direction.
		/// Impulse should be consumed after one tile if indoors,
		/// and last indefinitely (until hit by obstacle) if you pushed someone into deep space 
		[Server]
		public void Push(Vector2Int direction)
		{
			if (direction == Vector2Int.zero)
			{
				Logger.Log("Push with zero impulse??", Categories.PushPull);
				return;
			}

			serverState.Impulse = direction;
			serverTargetState.Impulse = direction;
			if (matrix != null)
			{
				Vector3Int pushGoal =
					Vector3Int.RoundToInt(serverState.Position + (Vector3) serverTargetState.Impulse);
				if (matrix.IsPassableAt(pushGoal))
				{
					Logger.Log($"Server push to {pushGoal}", Categories.PushPull);
					serverTargetState.Position = pushGoal;
					serverTargetState.ImportantFlightUpdate = true;
					serverTargetState.ResetClientQueue = true;
				}
				else
				{
					serverState.Impulse = Vector2.zero;
					serverTargetState.Impulse = Vector2.zero;
				}
			}
		}

		/// Manually set player to a specific world position.
		/// Also clears prediction queues.
		/// <param name="worldPos">The new position to "teleport" player</param>
		[Server]
		public void SetPosition(Vector3 worldPos)
		{
			ClearQueueServer();
			Vector3Int roundedPos = Vector3Int.RoundToInt((Vector2) worldPos); //cutting off z-axis
			MatrixInfo newMatrix = MatrixManager.AtPoint(roundedPos);
			//Note the client queue reset
			var newState = new PlayerState
			{
				MoveNumber = 0,
				MatrixId = newMatrix.Id,
				WorldPosition = roundedPos,
				ResetClientQueue = true
			};
			serverState = newState;
			serverTargetState = newState;
			SyncMatrix();
			NotifyPlayers();
		}

		///	When lerp is finished, inform players of new state  
		[Server]
		private void TryNotifyPlayers()
		{
			if (ServerPositionsMatch)
			{
//				When serverState reaches its planned destination,
//				embrace all other updates like updated moveNumber and flags
				serverState = serverTargetState;
				SyncMatrix();
				NotifyPlayers();
			}
		}

		/// Register player to matrix from serverState (ParentNetId is a SyncVar) 
		[Server]
		private void SyncMatrix()
		{
			registerTile.ParentNetId = MatrixManager.Get(serverState.MatrixId).NetId;
		}

		/// Send current serverState to just one player
		/// <param name="recipient">whom to inform</param>
		/// <param name="noLerp">(for init) tells client to do no lerping when changing pos this time</param>
		[Server]
		public void NotifyPlayer(GameObject recipient, bool noLerp = false) {
			serverState.NoLerp = noLerp;
			PlayerMoveMessage.Send(recipient, gameObject, serverState);
		}

		/// Send current serverState to all players
		[Server]
		public void NotifyPlayers(bool noLerp = false)
		{
			//Generally not sending mid-flight updates (unless there's a sudden change of course etc.)
			if (!serverState.ImportantFlightUpdate && consideredFloatingServer)
			{
				return;
			}
			serverState.NoLerp = noLerp;
			PlayerMoveMessage.SendToAll(gameObject, serverState);
			//Clearing state flags
			serverTargetState.ImportantFlightUpdate = false;
			serverTargetState.ResetClientQueue = false;
			serverState.ImportantFlightUpdate = false;
			serverState.ImportantFlightUpdate = false;
		}

		/// Clears server pending actions queue
		private void ClearQueueServer()
		{
//			Logger.Log("Server queue wiped!");
			if (serverPendingActions != null && serverPendingActions.Count > 0)
			{
				serverPendingActions.Clear();
			}
		}

		/// Clear all queues and
		/// inform players of true serverState
		[Server]
		private void RollbackPosition()
		{
			SetPosition(serverState.WorldPosition);
		}

		/// try getting moves from server queue if server and target states match
		[Server]
		private void CheckTargetUpdate()
		{
			//checking only player movement for now
			if (ServerPositionsMatch)
			{
				TryUpdateServerTarget();
			}
		}

		///Currently used to set the pos of a player that has just been dragged by another player
		//Fixme: prone to exploits, very hacky
		[Command]
		public void CmdSetPositionFromReset(GameObject fromObj, GameObject otherPlayer, Vector3 setPos)
		{
			if (fromObj.GetComponent<IPlayerSync>() == null) //Validation
			{
				return;
			}

			IPlayerSync otherPlayerSync = otherPlayer.GetComponent<IPlayerSync>();
			otherPlayerSync.SetPosition(setPos);
		}

		/// Tries to assign next target from queue to serverTargetState if there are any
		/// (In order to start lerping towards it)
		[Server]
		private void TryUpdateServerTarget()
		{
			if (serverPendingActions.Count == 0)
			{
				return;
			}

			var nextAction = serverPendingActions.Peek();
			if (!IsPointlessMove(serverTargetState, nextAction))
			{
				if (consideredFloatingServer)
				{
					Logger.LogWarning("Server ignored move while player is floating", Categories.Movement);
					serverPendingActions.Dequeue();
					return;
				}

				PlayerState nextState = NextStateServer(serverTargetState, serverPendingActions.Dequeue());
				serverLastDirection = Vector2Int.RoundToInt(nextState.WorldPosition - serverTargetState.WorldPosition);
				serverTargetState = nextState;
				//In case positions already match
				TryNotifyPlayers();
//				Logger.Log($"Server Updated target {serverTargetState}. {serverPendingActions.Count} pending");
			}
			else
			{
				Logger.LogWarning(
					$"Pointless move {serverTargetState}+{nextAction.keyCodes[0]} Rolling back to {serverState}",Categories.Movement);
				RollbackPosition();
			}
		}

		/// NextState that also subscribes player to matrix rotations 
		[Server]
		private PlayerState NextStateServer(PlayerState state, PlayerAction action)
		{
			bool matrixChangeDetected;
			PlayerState nextState = NextState(state, action, out matrixChangeDetected);

			if (!matrixChangeDetected)
			{
				return nextState;
			}

			//todo: subscribe to current matrix rotations on spawn
			var newMatrix = MatrixManager.Get(nextState.MatrixId);
			Logger.Log($"Matrix will change to {newMatrix}",Categories.Movement);
			if (newMatrix.MatrixMove)
			{
				//Subbing to new matrix rotations
				newMatrix.MatrixMove.OnRotate.AddListener( OnRotation );
//				Logger.Log( $"Registered rotation listener to {newMatrix.MatrixMove}" );
			}

			//Unsubbing from old matrix rotations
			MatrixMove oldMatrixMove = MatrixManager.Get(matrix).MatrixMove;
			if (oldMatrixMove)
			{
//				Logger.Log( $"Unregistered rotation listener from {oldMatrixMove}" );
				oldMatrixMove.OnRotate.RemoveListener( OnRotation );
			}

			return nextState;
		}

		[Server]
		private void OnRotation(Orientation from, Orientation to)
		{
			//fixme: doesn't seem to change orientation for clients from their point of view
			playerSprites.ChangePlayerDirection(Orientation.DegreeBetween(from, to));
		}

		/// Lerping and ensuring server authority for space walk
		[Server]
		private void CheckMovementServer()
		{
			if (!ServerPositionsMatch)
			{
				//Lerp on server if it's worth lerping 
				//and inform players if serverState reached targetState afterwards 
				serverState.WorldPosition =
					Vector3.MoveTowards(serverState.WorldPosition, serverTargetState.WorldPosition, playerMove.speed * Time.deltaTime);
				//failsafe
				var distance = Vector3.Distance(serverState.WorldPosition, serverTargetState.WorldPosition);
				if (distance > 1.5)
				{
					Logger.LogWarning($"Dist {distance} > 1:{serverState}\n" +
						$"Target    :{serverTargetState}",Categories.Movement);
					serverState.WorldPosition = serverTargetState.WorldPosition;
				}

				TryNotifyPlayers();
			}

			//Space walk checks
			if (IsInSpace)
			{
				if (serverTargetState.Impulse == Vector2.zero && serverLastDirection != Vector2.zero)
				{
					//server initiated space dive. 						
					serverTargetState.Impulse = serverLastDirection;
					serverTargetState.ImportantFlightUpdate = true;
					serverTargetState.ResetClientQueue = true;
				}

				//Perpetual floating sim
				if (ServerPositionsMatch && !serverTargetState.ImportantFlightUpdate)
				{
					//Extending prediction by one tile if player's transform reaches previously set goal
					Vector3Int newGoal = Vector3Int.RoundToInt(serverTargetState.Position + (Vector3) serverTargetState.Impulse);
					serverTargetState.Position = newGoal;
					ClearQueueServer();
				}
			}

			if (consideredFloatingServer && !IsInSpace)
			{
				//finish floating. players will be notified as soon as serverState catches up
				serverState.Impulse = Vector2.zero;
				serverTargetState.Impulse = Vector2.zero;
				serverTargetState.ResetClientQueue = true;

				//Stopping spacewalk increases move number
				serverTargetState.MoveNumber++;

				//removing lastDirection when we hit an obstacle in space
				serverLastDirection = Vector2.zero;

				//Notify if position stayed the same
				TryNotifyPlayers();
			}

			CheckSpaceDamage();
		}

		/// Checking whether player should suffocate
		[Server]
		private void CheckSpaceDamage()
		{
			if (MatrixManager.IsSpaceAt(Vector3Int.RoundToInt(serverState.WorldPosition))
			    && !healthBehaviorScript.IsDead && !isApplyingSpaceDmg)
			{
				// Hurting people in space even if they are next to the wall
				StartCoroutine(ApplyTempSpaceDamage());
				isApplyingSpaceDmg = true;
			}
		}

		// TODO: Remove this when atmos is implemented 
		// This prevents players drifting into space indefinitely 
		private IEnumerator ApplyTempSpaceDamage()
		{
			yield return new WaitForSeconds(1f);
			healthBehaviorScript.ApplyDamage(null, 5, DamageType.OXY, BodyPartType.HEAD);
			isApplyingSpaceDmg = false;
		}
	}
}