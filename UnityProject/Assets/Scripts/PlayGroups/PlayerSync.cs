using System;
using System.Collections;
using System.Collections.Generic;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
    /// Container with player position, flight direction etc. 
    /// Gives client enough information for smooth simulation
    public struct PlayerState
    {
        public int MoveNumber;
        public Vector3 Position;

        ///Direction of flying
        public Vector2 Impulse;

        ///Flag for clients to reset their queue when received 
        public bool ResetClientQueue;

        /// Flag for server to ensure that clients receive that flight update: 
        /// Only important flight updates (ones with impulse) are being sent out by server (usually start only)
        [NonSerialized] public bool ImportantFlightUpdate;

        public override string ToString() {
            return
                $"{nameof( MoveNumber )}: {MoveNumber}, {nameof( Position )}: {Position}, {nameof( Impulse )}: {Impulse}, " +
                $"reset: {ResetClientQueue}, flight: {ImportantFlightUpdate}";
        }
    }

    public struct PlayerAction
    {
        public int[] keyCodes;

        //clone of PlayerMove GetMoveDirection stuff
        //but there should be a way to see the direction of these keycodes ffs
        public Vector2Int Direction() {
            Vector2Int direction = Vector2Int.zero;
            for ( var i = 0; i < keyCodes.Length; i++ ) {
                direction += GetMoveDirection( (KeyCode) keyCodes[i] );
            }
            direction.x = Mathf.Clamp( direction.x, -1, 1 );
            direction.y = Mathf.Clamp( direction.y, -1, 1 );

            return direction;
        }

        private static Vector2Int GetMoveDirection( KeyCode action ) {
            switch ( action ) {
                case KeyCode.Z:
                case KeyCode.W:
                case KeyCode.UpArrow:
                    return Vector2Int.up;
                case KeyCode.A:
                case KeyCode.Q:
                case KeyCode.LeftArrow:
                    return Vector2Int.left;
                case KeyCode.S:
                case KeyCode.DownArrow:
                    return Vector2Int.down;
                case KeyCode.D:
                case KeyCode.RightArrow:
                    return Vector2Int.right;
            }
            return Vector2Int.zero;
        }
    }

    public partial class PlayerSync : NetworkBehaviour, IPlayerSync
    {
        ///For server code. Contains position
        public PlayerState ServerState => serverState;
        
        /// For client code
        public PlayerState ClientState => playerState;

//		private bool canRegister = false;
        private HealthBehaviour healthBehaviorScript;

        public PlayerMove playerMove;
        private PlayerScript playerScript;
        private PlayerSprites playerSprites;

        private Matrix matrix => registerTile.Matrix;

        public LayerMask matrixLayerMask;

        private RaycastHit2D[] rayHit;

        public GameObject PullingObject { get; set; }

        public NetworkInstanceId PullObjectID {
            get { return pullObjectID; }
            set { pullObjectID = value; }
        }

        //pull objects
        [SyncVar( hook = nameof( PullReset ) )] private NetworkInstanceId pullObjectID;

        private Vector3 pullPos;

        private RegisterTile pullRegister;

        private PushPull pushPull; //The pushpull component on this player

        private RegisterTile registerTile;

        private bool IsPointlessMove( PlayerState state, PlayerAction action ) {
            return state.Position.Equals( NextState( state, action ).Position );
        }

        private IEnumerator WaitForLoad() {
            yield return new WaitForEndOfFrame();
            if ( serverStateCache.Position != Vector3.zero && !isLocalPlayer ) {
                playerState = serverStateCache;
                transform.localPosition = Vector3Int.RoundToInt( playerState.Position );
            } else {
                PlayerState state = new PlayerState {MoveNumber = 0, Position = transform.localPosition};
                playerState = state;
                predictedState = state;
            }
            yield return new WaitForSeconds( 2f );

            PullReset( PullObjectID );
        }

        private void Start() {
            //Init pending actions queue for your local player
            if ( isLocalPlayer ) {
                pendingActions = new Queue<PlayerAction>();
                UpdatePredictedState();
            }
            //Init pending actions queue for server 
            if ( isServer ) {
                serverPendingActions = new Queue<PlayerAction>();
            }
            playerScript = GetComponent<PlayerScript>();
            playerSprites = GetComponent<PlayerSprites>();
            healthBehaviorScript = GetComponent<HealthBehaviour>();
            registerTile = GetComponent<RegisterTile>();
            pushPull = GetComponent<PushPull>();
        }

        private void Update() {
            if ( isLocalPlayer && playerMove != null ) {
                // If being pulled by another player and you try to break free
                //TODO Condition to check for handcuffs / straight jacket 
                // (probably better to adjust allowInput or something)
                if ( pushPull.pulledBy != null && !playerMove.isGhost ) {
                    for ( int i = 0; i < playerMove.keyCodes.Length; i++ ) {
                        if ( Input.GetKey( playerMove.keyCodes[i] ) ) {
                            playerScript.playerNetworkActions.CmdStopOtherPulling( gameObject );
                        }
                    }
                    return;
                }
                if ( predictedState.Position == transform.localPosition && !playerMove.isGhost ) {
                    DoAction();
                } else if ( predictedState.Position == playerScript.ghost.transform.localPosition &&
                            playerMove.isGhost ) {
                    DoAction();
                }
            }

            Synchronize();
        }

        private void RegisterObjects() {
            //Register playerpos in matrix
            registerTile.UpdatePosition();
            //Registering objects being pulled in matrix
            if ( pullRegister != null ) {
                pullRegister.UpdatePosition();
            }
        }

        private void Synchronize() {
            if ( isLocalPlayer && GameData.IsHeadlessServer ) {
                return;
            }
            if ( isServer ) {
                CheckTargetUpdate();
            }

            PlayerState state = isLocalPlayer ? predictedState : playerState;
            if ( !playerMove.isGhost ) {
                CheckSpaceWalk();

                if ( isLocalPlayer && playerMove.IsPushing || pushPull.pulledBy != null ) {
                    return;
                }

                if ( state.Position != transform.localPosition ) {
                    PlayerLerp( state );
                }

                if ( isServer && !ServerPositionsMatch ) {
                    //Lerp on server if it's worth lerping 
                    //and inform players if serverState reached targetState afterwards 
                    ServerLerp();
                    TryNotifyPlayers();
                }

                //Check if we should still be displaying an ItemListTab and update it, if so.
                ControlTabs.CheckItemListTab();

                if ( PullingObject != null ) {
                    if ( transform.hasChanged ) {
                        transform.hasChanged = false;
                        PullObject();
                    } else if ( PullingObject.transform.localPosition != pullPos ) {
                        PullingObject.transform.localPosition = pullPos;
                    }
                }

                //Registering
                if ( registerTile.Position != Vector3Int.RoundToInt( state.Position ) ) {
                    RegisterObjects();
                }
            } else {
                if ( state.Position != playerScript.ghost.transform.localPosition ) {
                    GhostLerp( state );
                }
            }
        }

        private void ClearStateFlags() {
            serverTargetState.ImportantFlightUpdate = false;
            serverTargetState.ResetClientQueue = false;
            serverState.ImportantFlightUpdate = false;
            serverState.ImportantFlightUpdate = false;
        }

        private void GhostLerp( PlayerState state ) {
            playerScript.ghost.transform.localPosition =
                Vector3.MoveTowards( playerScript.ghost.transform.localPosition,
                    state.Position,
                    playerMove.speed * Time.deltaTime );
        }

        private void PlayerLerp( PlayerState state ) {
            transform.localPosition = Vector3.MoveTowards( transform.localPosition,
                state.Position,
                playerMove.speed * Time.deltaTime );
        }

        private void PullObject() {
            pullPos = transform.localPosition - (Vector3) LastDirection;
            pullPos.z = PullingObject.transform.localPosition.z;

            Vector3Int pos = Vector3Int.RoundToInt( pullPos );
            if ( matrix.IsPassableAt( pos ) || matrix.ContainsAt( pos, gameObject ) ||
                 matrix.ContainsAt( pos, PullingObject ) ) {
                float journeyLength = Vector3.Distance( PullingObject.transform.localPosition, pullPos );
                if ( journeyLength <= 2f ) {
                    PullingObject.transform.localPosition =
                        Vector3.MoveTowards( PullingObject.transform.localPosition,
                            pullPos,
                            playerMove.speed * Time.deltaTime / journeyLength );
                } else {
                    //If object gets too far away activate warp speed
                    PullingObject.transform.localPosition =
                        Vector3.MoveTowards( PullingObject.transform.localPosition,
                            pullPos,
                            playerMove.speed * Time.deltaTime * 30f );
                }
                PullingObject.BroadcastMessage( "FaceDirection",
                    playerSprites.currentDirection,
                    SendMessageOptions.DontRequireReceiver );
            }
        }

        public void PullReset( NetworkInstanceId netID ) {
            PullObjectID = netID;

            transform.hasChanged = false;
            if ( netID == NetworkInstanceId.Invalid ) {
                if ( PullingObject != null ) {
                    pullRegister.UpdatePosition();
                    //Could be another player
                    PlayerSync otherPlayerSync = PullingObject.GetComponent<PlayerSync>();
                    if ( otherPlayerSync != null ) {
                        CmdSetPositionFromReset( gameObject,
                            otherPlayerSync.gameObject,
                            PullingObject.transform.localPosition );
                    }
                }
                pullRegister = null;
                PullingObject = null;
            } else {
                PullingObject = ClientScene.FindLocalObject( netID );
                PushPull oA = PullingObject.GetComponent<PushPull>();
                pullPos = PullingObject.transform.localPosition;
                if ( oA != null ) {
                    oA.pulledBy = gameObject;
                }
                pullRegister = PullingObject.GetComponent<RegisterTile>();
            }
        }

        private PlayerState NextState( PlayerState state, PlayerAction action, bool isReplay = false ) {
            return new PlayerState {
                MoveNumber = state.MoveNumber + 1,
                Position = playerMove.GetNextPosition( Vector3Int.RoundToInt( state.Position ), action, isReplay )
            };
        }

        public void ProcessAction( PlayerAction action ) {
            CmdProcessAction( action );
        }

        /// Space walk checks for client and server
        private void CheckSpaceWalk() {
            if ( matrix == null ) {
                return;
            }
            //Server zone
            if ( isServer ) {
                CheckSpaceWalkServer();
            }
            //Client zone
            CheckSpaceWalkClient();
        }

        //Visual debug
        private Vector3 size1 = Vector3.one;
        private Vector3 size2 = new Vector3( 0.9f, 0.9f, 0.9f );
        private Vector3 size3 = new Vector3( 0.8f, 0.8f, 0.8f );
        private Vector3 size4 = new Vector3( 0.7f, 0.7f, 0.7f );
        private Color color1 = Color.red;
        private Color color2 = DebugTools.HexToColor( "fd7c6e" );
        private Color color3 = DebugTools.HexToColor( "22e600" );
        private Color color4 = DebugTools.HexToColor( "ebfceb" );

        private void OnDrawGizmos() {
            //serverTargetState
            Gizmos.color = color1;
            Gizmos.DrawWireCube( serverTargetState.Position - CustomNetTransform.deOffset, size1 );

            //serverState
            Gizmos.color = color2;
            Gizmos.DrawWireCube( serverState.Position - CustomNetTransform.deOffset, size2 );

            //client predictedState
            Gizmos.color = color3;
            Vector3 clientPrediction = predictedState.Position - CustomNetTransform.deOffset;
            Gizmos.DrawWireCube( clientPrediction, size3 );
            GizmoUtils.DrawArrow( clientPrediction + Vector3.left / 5, predictedState.Impulse );
            GizmoUtils.DrawText( predictedState.MoveNumber.ToString(), clientPrediction + Vector3.left, 15 );

            //client playerState
            Gizmos.color = color4;
            Vector3 clientState = playerState.Position - CustomNetTransform.deOffset;
            Gizmos.DrawWireCube( clientState, size4 );
            GizmoUtils.DrawArrow( clientState + Vector3.right / 5, playerState.Impulse );
            GizmoUtils.DrawText( playerState.MoveNumber.ToString(), clientState + Vector3.right, 15 );
        }
    }
}