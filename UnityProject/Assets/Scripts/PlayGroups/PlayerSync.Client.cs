using System.Collections.Generic;
using UnityEngine;

namespace PlayGroup
{
    public partial class PlayerSync
    {
        //Client-only fields, don't concern server
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
                if ( value != Vector2.zero ) {
//					Debug.Log($"Setting lastDirection to {value}");
                    lastDirection = value;
                } else {
                    Debug.LogWarning( "Attempt to set lastDirection to zero!" );
                }
            }
        }

        ///Does server claim this client is floating rn?
        private bool isFloatingClient => playerState.Impulse != Vector2.zero;

        /// Does your client think you should be floating rn? (Regardless of what server thinks)
        private bool isPseudoFloatingClient => predictedState.Impulse != Vector2.zero;

        /// Measure to avoid lerping back and forth in a lagspike 
        /// where player simulated entire spacewalk (start and stop) without getting server's answer yet
        private bool blockClientMovement = false;

        public override void OnStartClient() {
            StartCoroutine( WaitForLoad() );
            base.OnStartClient();
        }

        private void DoAction() {
            PlayerAction action = playerMove.SendAction();
            if ( action.keyCodes.Length != 0 && !IsPointlessMove( predictedState, action ) ) {
//				Debug.Log($"Client requesting {action} ({pendingActions.Count} in queue)");

                //experiment: not enqueueing or processing action if floating.
                //arguably it shouldn't really be like that in the future 
                if ( !isPseudoFloatingClient && !isFloatingClient && !blockClientMovement ) {
                    pendingActions.Enqueue( action );

                    LastDirection = action.Direction();
                    UpdatePredictedState();

                    //Seems like Cmds are reliable enough in this case
                    CmdProcessAction( action );
                    //				RequestMoveMessage.Send(action); 
                }
            }
        }

        private void UpdatePredictedState() {
            if ( pendingActions.Count == 0 ) {
                //plain assignment if there's nothing to predict
                predictedState = playerState;
            } else {
                //redraw prediction point from received serverState using pending actions
                PlayerState tempState = playerState;
                int curPredictedMove = predictedState.MoveNumber;

                foreach ( PlayerAction action in pendingActions ) {
                    //isReplay determines if this action is a replayed action for use in the prediction system
                    bool isReplay = predictedState.MoveNumber <= curPredictedMove;
                    tempState = NextState( tempState, action, isReplay );
                }
                predictedState = tempState;
//				Debug.Log($"Redraw prediction: {playerState}->{predictedState}({pendingActions.Count} steps) ");
            }
        }

        /// Called when PlayerMoveMessage is received
        public void UpdateClientState( PlayerState state ) {
            playerState = state;
//            Debug.Log( $"Got server update {playerState}" );

            if ( blockClientMovement ) {
                if ( isFloatingClient ) {
                    Debug.Log( "Your last trip got approved, yay!" );
                    ClearQueueClient();
                    blockClientMovement = false;
                } else {
                    Debug.LogWarning( "Movement blocked. Waiting for a sign of approval for experienced flight" );
                    return;
                }
            }
            if ( isFloatingClient ) {
                LastDirection = playerState.Impulse;
            }

            //don't reset predicted state if it guessed impulse correctly 
            //or server is just approving old moves when you weren't flying yet
            if ( isFloatingClient || isPseudoFloatingClient ) {
                bool shouldReset = predictedState.Impulse != playerState.Impulse &&
                                   predictedState.MoveNumber == playerState.MoveNumber;
                if ( shouldReset ) {
//                    Debug.Log( $"Reset predictedState {predictedState} with {playerState}" );
                    predictedState = playerState;
                }
                return;
            }
            if ( pendingActions != null ) {
                //invalidate queue if serverstate was never predicted
                bool serverAhead = playerState.MoveNumber > predictedState.MoveNumber;
                bool posMismatch = playerState.MoveNumber == predictedState.MoveNumber
                                   && playerState.Position != predictedState.Position;
                if ( serverAhead || posMismatch ) {
                    Debug.LogWarning( $"serverAhead={serverAhead}, posMismatch={posMismatch}" );
                    ClearQueueClient();
                    predictedState = playerState;
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

        /// Clears client pending actions queue
        public void ClearQueueClient() {
//			Debug.Log("Resetting queue as requested by server!");
            if ( pendingActions != null && pendingActions.Count > 0 ) {
                pendingActions.Clear();
            }
        }

        ///Simulate space walk by server's orders or initiate/stop them on client
        ///Using predictedState for your own player and playerState for others
        private void CheckSpaceWalkClient() {
            PlayerState state = isLocalPlayer ? predictedState : playerState;
            Vector3Int pos = Vector3Int.RoundToInt( state.Position );
            if ( isPseudoFloatingClient && !matrix.IsFloatingAt( pos ) ) {
//                Debug.Log( "Stopped clientside floating to avoid going through walls" );
                //stop floating on client (if server isn't responding in time) to avoid players going through walls
                predictedState.Impulse = Vector2.zero;
                //Stopping spacewalk increases move number
                predictedState.MoveNumber++;
                if ( !isFloatingClient && playerState.MoveNumber < predictedState.MoveNumber ) {
                    Debug.Log( "Got an unapproved flight here!" );
                    //Client figured out that he just finished spacewalking 
                    //and server is yet to approve the fact that it even started.
                    //Marking as UnapprovedFloatClient 
                    //to ignore further predictive movement until flight approval message is received
                    blockClientMovement = true;
                }
            }
            if ( matrix.IsFloatingAt( pos ) ) {
                if ( state.Impulse == Vector2.zero && LastDirection != Vector2.zero ) {
                    //client initiated space dive. 						
                    state.Impulse = LastDirection;
                    if ( isLocalPlayer ) {
                        predictedState.Impulse = state.Impulse;
                    } else {
                        playerState.Impulse = state.Impulse;
                    }
//					Debug.Log($"Wasn't floating on client, now floating with impulse {LastDirection}. FC={isFloatingClient},PFC={isPseudoFloatingClient}");
                }

                //Perpetual floating sim
                if ( transform.localPosition == state.Position ) {
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
}