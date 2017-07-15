using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;

namespace PlayGroup {

    public class PlayerMove: NetworkBehaviour {

        private PlayerSprites playerSprites;
        private PlayerSync playerSync;

        private static KeyCode[] keyCodes = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.RightArrow };

        public bool diagonalMovement;
        public float speed = 10;
		public bool isInSpace = false;
		[SyncVar]
		public bool allowInput = true;
		[SyncVar]
		public bool isGhost = false;
		[HideInInspector]
		public bool isPushing = false;

        private List<KeyCode> pressedKeys = new List<KeyCode>();

        void Start() {
            playerSprites = gameObject.GetComponent<PlayerSprites>();
            playerSync = GetComponent<PlayerSync>();
        }

        public PlayerAction SendAction() {
            var actionKeys = new List<int>();

            foreach(var keyCode in keyCodes) {
				if (PlayerManager.LocalPlayer == gameObject && UIManager.Chat.isChatFocus)
					return new PlayerAction() { keyCodes = actionKeys.ToArray() };
				
				if(Input.GetKey(keyCode) && allowInput && !isPushing) {
                    actionKeys.Add((int) keyCode);
                }
            }

            return new PlayerAction() { keyCodes = actionKeys.ToArray() };
        }

        public Vector3 GetNextPosition(Vector3 currentPosition, PlayerAction action) {
			if (isServer) {
				bool isSpace = Matrix.Matrix.At(currentPosition).IsSpace();
				if (isSpace && !isInSpace) {
					isInSpace = true;
				} else if (!isSpace && isInSpace) {
					isInSpace = false;
				}
			}

            var direction = GetDirection(action);
            if(!isGhost)
                playerSprites.FaceDirection(direction);
            
            var adjustedDirection = AdjustDirection(currentPosition, direction);

            if(adjustedDirection == Vector3.zero) {
                Interact(currentPosition, direction);
            }

            return currentPosition + adjustedDirection;
        }

        private Vector3 GetDirection(PlayerAction action) {
            ProcessAction(action);

            if(diagonalMovement) {
                return GetMoveDirection(pressedKeys);
            }
            if(pressedKeys.Count > 0) {
                return GetMoveDirection(pressedKeys[pressedKeys.Count - 1]);
            }
            return Vector3.zero;
        }

        private void ProcessAction(PlayerAction action) {
            var actionKeys = new List<int>(action.keyCodes);
            foreach(var keyCode in keyCodes) {
                if(actionKeys.Contains((int) keyCode) && !pressedKeys.Contains(keyCode)) {
                    pressedKeys.Add(keyCode);
                } else if(!actionKeys.Contains((int) keyCode) && pressedKeys.Contains(keyCode)) {
                    pressedKeys.Remove(keyCode);
                }
            }
        }

        private Vector3 GetMoveDirection(List<KeyCode> actions) {
            var direction = Vector3.zero;
            foreach(var keycode in pressedKeys) {
                direction += GetMoveDirection(keycode);
            }
            direction.x = Mathf.Clamp(direction.x, -1, 1);
            direction.y = Mathf.Clamp(direction.y, -1, 1);

            return direction;
        }

        private Vector3 GetMoveDirection(KeyCode action) {
			if (PlayerManager.LocalPlayer == gameObject && UIManager.Chat.isChatFocus)
				return Vector3.zero;
			
            switch(action) {
                case KeyCode.W:
                case KeyCode.UpArrow:
                    return Vector3.up;
                case KeyCode.A:
                case KeyCode.LeftArrow:
                    return Vector3.left;
                case KeyCode.S:
                case KeyCode.DownArrow:
                    return Vector3.down;
                case KeyCode.D:
                case KeyCode.RightArrow:
                    return Vector3.right;
            }

            return Vector3.zero;
        }

        private Vector3 AdjustDirection(Vector3 currentPosition, Vector3 direction) {
            var horizontal = Vector3.Scale(direction, Vector3.right);
            var vertical = Vector3.Scale(direction, Vector3.up);

			if (isGhost) {
				return direction;
			}
            Vector3 _pos = currentPosition + direction;
            if (Matrix.Matrix.At(currentPosition + direction).IsPassable() || Matrix.Matrix.At(currentPosition + direction).ContainsTile(gameObject))
            {
                return direction;
            }
            else if (playerSync.pullingObject != null)
            {
                if (Matrix.Matrix.At(currentPosition + direction).ContainsTile(playerSync.pullingObject))
                {
                        Vector2 directionToPullObj = playerSync.pullingObject.transform.position - transform.position;
                    if (directionToPullObj.normalized != playerSprites.currentDirection)
                    {
                        // Ran into pullObject but was not facing it, saved direction
                        return direction;
                    }
                    else
                    {
                        //Hit Pull obj
                        PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(playerSync.pullingObject);
                    }
                }
            }

            //could not pass
            return Vector3.zero;
        }
        private void Interact(Vector3 currentPosition, Vector3 direction) {
            var doorController = Matrix.Matrix.At(currentPosition + direction).GetDoor();
            if (doorController != null) {
                doorController.CmdTryOpen();
            }

			var objectActions = Matrix.Matrix.At(currentPosition + direction).GetObjectActions();
			if (objectActions != null) {
				objectActions.TryPush(gameObject, speed, direction);
			}
        }
    }
}