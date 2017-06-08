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
		private PlayerSprites playerSprites;
		private PlayerScript playerScript;
		private RegisterTile registerTile;

        private Queue<PlayerAction> pendingActions;

        [SyncVar(hook = "OnServerStateChange")] 
        private PlayerState serverState;
        private PlayerState predictedState;


        void Awake() {
            InitState();
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
			playerSprites = GetComponent<PlayerSprites>();
			playerScript = GetComponent<PlayerScript>();
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
				var state = isLocalPlayer ? predictedState : serverState;
				transform.position = Vector3.MoveTowards(transform.position, state.Position, playerMove.speed * Time.deltaTime);
				if (registerTile.savedPosition != state.Position) {
					registerTile.UpdateTile(state.Position);
				}
			} else {
				var state = isLocalPlayer ? predictedState : serverState;
				playerScript.ghost.transform.position = Vector3.MoveTowards(playerScript.ghost.transform.position, state.Position, playerMove.speed * Time.deltaTime);
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

        private void OnServerStateChange(PlayerState newState) {
            serverState = newState;

            if(pendingActions != null) {
                while(pendingActions.Count > (predictedState.MoveNumber - serverState.MoveNumber)) {
                    pendingActions.Dequeue();
                }
                UpdatePredictedState();
            }
        }
    }
}