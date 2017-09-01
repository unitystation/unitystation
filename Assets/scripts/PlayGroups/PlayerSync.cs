using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using Matrix;

namespace PlayGroup {

    public struct PlayerState {
        public int MoveNumber;
        public Vector3 Position;
    }

    public struct PlayerAction {
        public int[] keyCodes;
    }

    public class PlayerSync: NetworkBehaviour {

        public PlayerMove playerMove;
        private PlayerScript playerScript;
        private PlayerSprites playerSprites;
        private RegisterTile registerTile;
        private Queue<PlayerAction> pendingActions;

        [SyncVar(hook = "OnServerStateChange")]
        private PlayerState serverState;
        private PlayerState predictedState;

        //cache
        private PlayerState state;
        //pull objects
        [SyncVar(hook = "PullReset")]
        public NetworkInstanceId pullObjectID;
        public GameObject pullingObject;
        private RegisterTile pullRegister;
        private bool canRegister = false;
        private Vector3 pullPos;

        void Awake() {
            InitState();
        }

		public override void OnStartServer(){
			pullObjectID = NetworkInstanceId.Invalid;
			base.OnStartServer();
		}
        public override void OnStartClient() {
			StartCoroutine(WaitForLoad());
			base.OnStartClient();
        }

		IEnumerator WaitForLoad(){
			yield return new WaitForSeconds(2f);

			PullReset(pullObjectID);
		}

        [Server]
        private void InitState() {
            var position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0);
            serverState = new PlayerState() { MoveNumber = 0, Position = position };
        }

        void Start() {
            if(isLocalPlayer) {
                pendingActions = new Queue<PlayerAction>();
                UpdatePredictedState();
            }
            playerScript = GetComponent<PlayerScript>();
            playerSprites = GetComponent<PlayerSprites>();
            registerTile = GetComponent<RegisterTile>();
        }

        void Update() {
            if(isLocalPlayer && playerMove != null) {
                if(predictedState.Position == transform.position && !playerMove.isGhost) {
                    DoAction();
                } else if(predictedState.Position == playerScript.ghost.transform.position && playerMove.isGhost) {
                    DoAction();
                }
            }

            Synchronize();
        }

        private void RegisterObjects() {
            //Register playerpos in matrix
            registerTile.UpdateTile(state.Position);
            //Registering objects being pulled in matrix
            if(pullRegister != null) {
                Vector3 pos = state.Position - (Vector3) playerSprites.currentDirection;
                pullRegister.UpdateTile(pos);
            }
        }

        private void DoAction() {
            var action = playerMove.SendAction();
            if(action.keyCodes.Length != 0) {
                pendingActions.Enqueue(action);
                UpdatePredictedState();
                CmdAction(action);
            }
        }

        private void Synchronize() {
            if(isLocalPlayer && GameData.IsHeadlessServer)
                return;

            if(!playerMove.isGhost) {
                if(isLocalPlayer && playerMove.isPushing)
                    return;

                state = isLocalPlayer ? predictedState : serverState;
                transform.position = Vector3.MoveTowards(transform.position, state.Position, playerMove.speed * Time.deltaTime);

                if(pullingObject != null) {
                    if(transform.hasChanged) {
                        transform.hasChanged = false;
                        PullObject();
                    } else if(pullingObject.transform.position != pullPos) {
                        pullingObject.transform.position = pullPos;
                    }
                }

                //Registering
                if(registerTile.savedPosition != state.Position) {
                    RegisterObjects();
                }
            } else {
                var state = isLocalPlayer ? predictedState : serverState;
                playerScript.ghost.transform.position = Vector3.MoveTowards(playerScript.ghost.transform.position, state.Position, playerMove.speed * Time.deltaTime);
            }
        }

        private void PullObject() {
            pullPos = transform.position - (Vector3) playerSprites.currentDirection;
            pullPos.z = pullingObject.transform.position.z;
            if(Matrix.Matrix.At(pullPos).IsPassable() ||
                Matrix.Matrix.At(pullPos).ContainsTile(pullingObject) ||
                Matrix.Matrix.At(pullPos).ContainsTile(gameObject)) {
                float journeyLength = Vector3.Distance(pullingObject.transform.position, pullPos);
                if(journeyLength <= 2f) {
                    pullingObject.transform.position = Vector3.MoveTowards(pullingObject.transform.position, pullPos, (playerMove.speed * Time.deltaTime) / journeyLength);
                } else {
                    //If object gets too far away activate warp speed
                    pullingObject.transform.position = Vector3.MoveTowards(pullingObject.transform.position, pullPos, (playerMove.speed * Time.deltaTime) * 30f);
                }
            }
        }

        [Command]
        private void CmdAction(PlayerAction action) {
            serverState = NextState(serverState, action);
        }

        private void UpdatePredictedState() {

            predictedState = serverState;

            foreach(var action in pendingActions) {
                predictedState = NextState(predictedState, action);
            }
        }

        private PlayerState NextState(PlayerState state, PlayerAction action) {

            return new PlayerState() {
                MoveNumber = state.MoveNumber + 1,
                Position = playerMove.GetNextPosition(state.Position, action)
            };
        }

        public void PullReset(NetworkInstanceId netID) {
            pullObjectID = netID;
			if (netID == null)
				netID = NetworkInstanceId.Invalid;
			
            transform.hasChanged = false;
            if(netID == NetworkInstanceId.Invalid) {
                if(pullingObject != null) {
                    pullRegister.editModeControl.Snap();
                    pullRegister.UpdateTile(pullingObject.transform.position);
                    EditModeControl eM = pullingObject.GetComponent<EditModeControl>();
                    eM.Snap();
                }
                pullRegister = null;
                pullingObject = null;
            } else {
                pullingObject = ClientScene.FindLocalObject(netID);
                ObjectActions oA = pullingObject.GetComponent<ObjectActions>();
                pullPos = pullingObject.transform.position;
                if(oA != null) {
                    oA.pulledBy = gameObject;
                }
                pullRegister = pullingObject.GetComponent<RegisterTile>();
            }
        }

        private void OnServerStateChange(PlayerState newState) {
            serverState = newState;

            if(pendingActions != null) {
                while(pendingActions.Count > (predictedState.MoveNumber - serverState.MoveNumber)) {
                    pendingActions.Dequeue();
                }
                UpdatePredictedState();
            }
        }

        private Vector3 RoundedPos(Vector3 pos) {
            return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
        }
    }
}