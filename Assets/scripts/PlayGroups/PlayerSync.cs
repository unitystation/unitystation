using System.Collections.Generic;
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

        private PlayerMove playerMove;
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
		public NetworkInstanceId pullObjectID = NetworkInstanceId.Invalid;
        public GameObject pullingObject;
        private RegisterTile pullRegister;
        private bool canRegister = false;
		private Vector3 pullPos;

        void Awake() {
            InitState();
        }

		public override void OnStartClient()
		{
			PullReset(pullObjectID);
		}

        [Server]
        private void InitState() {
            var position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0);
            serverState = new PlayerState() { MoveNumber = 0, Position = position };
        }

        void Start() {
			//Temp solution for host in headless mode (hiding the player at Vector3.zero
			if (GameData.IsHeadlessServer && isLocalPlayer) {
				PlayerManager.LocalPlayer.transform.position = new Vector3(-1000f,-1000f,0f);
			}

            if(isLocalPlayer) {
                pendingActions = new Queue<PlayerAction>();
                UpdatePredictedState();
            }
            playerMove = GetComponent<PlayerMove>();
			playerScript = GetComponent<PlayerScript>();
            playerSprites = GetComponent<PlayerSprites>();
			registerTile = GetComponent<RegisterTile>();
        }

        void Update() {
            if(isLocalPlayer) {
				if (predictedState.Position == transform.position && !playerMove.isGhost) {
					DoAction();
				} else if (predictedState.Position == playerScript.ghost.transform.position && playerMove.isGhost) {
					DoAction();
				}
            }

            Synchronize();
        }

        void LateUpdate(){
            if (canRegister)
            {
                canRegister = false;
                RegisterObjects();
            }
        }

        private void RegisterObjects(){
            //Register playerpos in matrix
            registerTile.UpdateTile(state.Position);
            //Registering objects being pulled in matrix
            if (pullRegister != null)
            {
                Vector3 pos = RoundedPos(state.Position - (Vector3)playerSprites.currentDirection);
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
			if (isLocalPlayer && GameData.IsHeadlessServer)
				return;

			if (!playerMove.isGhost) {
				state = isLocalPlayer ? predictedState : serverState;
                transform.position = Vector3.MoveTowards(transform.position, state.Position, playerMove.speed * Time.deltaTime);

				if (pullingObject != null) {
					if (transform.hasChanged) {
						transform.hasChanged = false;
						PullObject();
					} else if (pullingObject.transform.position != pullPos) {
						pullingObject.transform.position = pullPos;
					}
				}

                //Registering
                if (registerTile.savedPosition != state.Position) {
                    canRegister = true;
				}
			} else {
				var state = isLocalPlayer ? predictedState : serverState;
				playerScript.ghost.transform.position = Vector3.MoveTowards(playerScript.ghost.transform.position, state.Position, playerMove.speed * Time.deltaTime);
			}
        }

        private void PullObject(){
            pullPos = transform.position - (Vector3)playerSprites.currentDirection;
            pullPos.z = pullingObject.transform.position.z;
            if (Matrix.Matrix.At(pullPos).IsPassable() || 
                Matrix.Matrix.At(pullPos).ContainsTile(pullingObject) || 
                Matrix.Matrix.At(pullPos).ContainsTile(gameObject))
            {
				float journeyLength = Vector3.Distance(pullingObject.transform.position, pullPos);
				pullingObject.transform.position = Vector3.MoveTowards(pullingObject.transform.position, pullPos, (playerMove.speed * Time.deltaTime) / journeyLength);
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

        private void PullReset(NetworkInstanceId netID){
            transform.hasChanged = false;
            if (netID == NetworkInstanceId.Invalid)
            {
                if (pullingObject != null)
                {
//                    NetworkTransform nT = pullingObject.GetComponent<NetworkTransform>();
//                    nT.enabled = true;
                    pullRegister.editModeControl.Snap();
                    pullRegister.UpdateTile(pullingObject.transform.position);
                    EditModeControl eM = pullingObject.GetComponent<EditModeControl>();
                    eM.Snap();
                }
                pullRegister = null;
                pullingObject = null;
            }
            else
            {
                pullingObject = ClientScene.FindLocalObject(netID);
                ObjectActions oA = pullingObject.GetComponent<ObjectActions>();
				pullPos = pullingObject.transform.position;
                if (oA != null)
                {
                    oA.pulledBy = gameObject;
                }
//                NetworkTransform nT = pullingObject.GetComponent<NetworkTransform>();
//                nT.enabled = false;
                pullRegister = pullingObject.GetComponent<RegisterTile>();
            }
        }
		void OnCollisionEnter2D (Collision2D coll){
			ObjectActions oA = coll.gameObject.GetComponent<ObjectActions>();
			if (oA != null) {
				oA.TryPush(gameObject, playerMove.speed, playerSprites.currentDirection);
			}
		}
        private void OnServerStateChange(PlayerState newState) {
			serverState = newState;

			if (pendingActions != null) {
				while (pendingActions.Count > (predictedState.MoveNumber - serverState.MoveNumber)) {
					pendingActions.Dequeue();
				}
				UpdatePredictedState();
			} 
        }

        private Vector3 RoundedPos(Vector3 pos)
        {
            return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
        }
    }
}