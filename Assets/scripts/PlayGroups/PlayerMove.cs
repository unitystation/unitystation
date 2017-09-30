using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using Doors;

namespace PlayGroup {

	/// <summary>
	/// Player move queues the directional move keys 
	/// to be processed along with the server.
	/// It also changes the sprite direction and 
	/// handles interaction with objects that can 
	/// be walked into it. 
	/// </summary>
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
        private bool _isPush;
        public bool isPushing { get{ return _isPush;} set {
                _isPush = value;} }

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

		/// <summary>
		/// Check current and next tiles to determine their status and if movement is allowed
		/// </summary>
        private Vector3 AdjustDirection(Vector3 currentPosition, Vector3 direction) {
			//TODO Spaced movement

            var horizontal = Vector3.Scale(direction, Vector3.right);
            var vertical = Vector3.Scale(direction, Vector3.up);

			if (isGhost) {
				return direction;
			}
			//Is the current tile restrictive?
			if(Matrix.Matrix.At(currentPosition).IsRestrictiveTile()){
				if(!Matrix.Matrix.At(currentPosition).GetMoveRestrictions().CheckAllowedDir(direction)){
					//could not pass
					return Vector3.zero;
				}
			}

			//Is the next tile restrictive?
			if (Matrix.Matrix.At(currentPosition + direction).IsRestrictiveTile()) {
				if (!Matrix.Matrix.At(currentPosition + direction).GetMoveRestrictions().CheckAllowedDir(-direction)) {
					//could not pass
					return Vector3.zero;
				}
			}
 
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

            if (doorController != null && allowInput) {
                //checking if the door actually has a restriction (only need one because that's how ss13 works!
                if (doorController.restriction >0)
                {   //checking if the ID slot on player contains an ID with an itemIdentity component
                    if (UIManager.InventorySlots.IDSlot.IsFull && UIManager.InventorySlots.IDSlot.Item.GetComponent<IDCard>() != null)
                    {   //checking if the ID has access to bypass the restriction
                        CheckDoorAccess(UIManager.InventorySlots.IDSlot.Item.GetComponent<IDCard>(), doorController);
                        //Check the current hand for an ID
                    }else if (UIManager.Hands.CurrentSlot.IsFull && UIManager.Hands.CurrentSlot.Item.GetComponent<IDCard>() != null)
                    {
                        CheckDoorAccess(UIManager.Hands.CurrentSlot.Item.GetComponent<IDCard>(), doorController);
                    }else
                    {//does not have an ID
                        allowInput = false;
                        StartCoroutine(DoorInputCoolDown());
                        if(CustomNetworkManager.Instance._isServer)
                            doorController.CmdTryDenied();
                    }
                }
                else
                {//door does not have restriction
                    allowInput = false;
					//Server only here but it is a cmd for the input trigger (opening with mouse click from client)
					if(CustomNetworkManager.Instance._isServer)
                    doorController.CmdTryOpen(gameObject);
					
                    StartCoroutine(DoorInputCoolDown());
                }
            }

			var objectActions = Matrix.Matrix.At(currentPosition + direction).GetPushPull();
			if (objectActions != null) {
				objectActions.TryPush(gameObject, speed, direction);
			}
        }

        void CheckDoorAccess(IDCard cardID, DoorController doorController){
			if (cardID.accessSyncList.Contains((int)doorController.restriction))
            {// has access
                allowInput = false;
                //Server only here but it is a cmd for the input trigger (opening with mouse click from client)
                if(CustomNetworkManager.Instance._isServer)
                    doorController.CmdTryOpen(gameObject);

                StartCoroutine(DoorInputCoolDown());
            }else
            {// does not have access
                allowInput = false;
                StartCoroutine(DoorInputCoolDown());
                //Server only here but it is a cmd for the input trigger (opening with mouse click from client)
                if(CustomNetworkManager.Instance._isServer)
                    doorController.CmdTryDenied();
            }
        }

        //FIXME an ugly temp fix for an ugly problem. Will implement callbacks after 0.1.3
        IEnumerator DoorInputCoolDown(){
            yield return new WaitForSeconds(0.3f);
            allowInput = true;
        }

    }
}