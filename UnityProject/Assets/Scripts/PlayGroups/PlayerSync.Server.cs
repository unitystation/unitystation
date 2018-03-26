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

        private bool isFloatingServer => serverState.Impulse != Vector2.zero;

        /// idk if it's robust enough, but it seems to work
        private bool ServerPositionsMatch => serverTargetState.Position == serverState.Position;

        public override void OnStartServer() {
            PullObjectID = NetworkInstanceId.Invalid;
            InitServerState();
            base.OnStartServer();
        }

        [Server]
        private void InitServerState() {
            Vector3Int position = Vector3Int.RoundToInt( transform.localPosition );
            PlayerState state = new PlayerState {MoveNumber = 0, Position = position};
            serverState = state;
            serverStateCache = state;
            serverTargetState = state;
        }

        [Command( channel = 0 )]
        private void CmdProcessAction( PlayerAction action ) {
            //add action to server simulation queue
            serverPendingActions.Enqueue( action );
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

            //Do not cache the position if the player is a ghost
            //or else new players will sync the deadbody with the last pos
            //of the gost:
            if ( !playerMove.isGhost ) {
                serverStateCache = serverState;
            }
        }

        /// Push player in direction.
        /// Impulse should be consumed after one tile if indoors,
        /// and last indefinitely (until hit by obstacle) if you pushed someone into deep space 
        [Server]
        public void Push( Vector2Int direction ) {
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

        /// <summary>
        ///     Manually set a player to a specific position.
        /// 	Also clears prediction queues.
        /// </summary>
        /// <param name="pos">The new position to "teleport" player</param>
        [Server]
        public void SetPosition( Vector3 pos ) {
            ClearQueueServer();
            Vector3Int roundedPos = Vector3Int.RoundToInt( pos );
            //Note the client queue reset
            var newState = new PlayerState {
                MoveNumber = 0,
                Position = roundedPos,
                ResetClientQueue = true
            };
            serverState = newState;
            serverTargetState = newState;
            serverStateCache = newState;
            NotifyPlayers();
        }

        ///	When lerp is finished, inform players of new state  
        [Server]
        private void TryNotifyPlayers() {
            if ( ServerPositionsMatch ) {
//				When serverState reaches its planned destination,
//				embrace all other updates like updated moveNumber and flags
                serverState = serverTargetState;
                NotifyPlayers();
            }
        }

        [Server]
        private void NotifyPlayers() {
            //Do not cache the position if the player is a ghost
            //or else new players will sync the deadbody with the last pos
            //of the ghost:
            if ( !playerMove.isGhost ) {
                serverStateCache = serverState;
            }
            //Generally not sending mid-flight updates (unless there's a sudden change of course etc.)
            if ( !serverState.ImportantFlightUpdate && isFloatingServer ) {
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

        [Server]
        private void ServerLerp() {
            serverState.Position =
                Vector3.MoveTowards( serverState.Position,
                    serverTargetState.Position,
                    playerMove.speed * Time.deltaTime );
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
                if ( isFloatingServer ) {
                    Debug.LogWarning( "Server ignored move while player is floating" );
                    serverPendingActions.Dequeue();
                    return;
                }
                PlayerState nextState = NextState( serverTargetState, serverPendingActions.Dequeue() );
                serverLastDirection = Vector2Int.RoundToInt( nextState.Position - serverTargetState.Position );
                serverTargetState = nextState;
//				Debug.Log($"Server Updated target {serverTargetState}. {serverPendingActions.Count} pending");
            } else {
                Debug.LogWarning(
                    $"Pointless move {serverTargetState}+{nextAction.keyCodes[0]} Rolling back to {serverState}" );
                RollbackPosition();
            }
        }

        /// Ensuring server authority for space walk
        [Server]
        private void CheckSpaceWalkServer() {
            if ( matrix.IsFloatingAt( Vector3Int.RoundToInt( serverTargetState.Position ) ) ) {
                if ( !isFloatingServer ) {
                    //initiate floating
                    //notify players that we started floating
                    Push( Vector2Int.RoundToInt( serverLastDirection ) );
                } else if ( ServerPositionsMatch && !serverTargetState.ImportantFlightUpdate ) {
                    //continue floating
                    serverTargetState.Position =
                        Vector3Int.RoundToInt( serverState.Position + (Vector3) serverTargetState.Impulse );
                    ClearQueueServer();
                }
            } else if ( isFloatingServer ) {
                //finish floating. players will be notified as soon as serverState catches up
                serverState.Impulse = Vector2.zero;
                serverTargetState.Impulse = Vector2.zero;
                serverTargetState.ResetClientQueue = true;
                //Stopping spacewalk increases move number
                serverTargetState.MoveNumber++;
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
			healthBehaviorScript.ApplyDamage(null, 5, DamageType.OXY, BodyPartType.HEAD);
            isApplyingSpaceDmg = false;
        }
    }
}