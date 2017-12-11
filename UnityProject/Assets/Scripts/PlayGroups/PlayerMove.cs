using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using Doors;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;

namespace PlayGroup
{
    /// <summary>
    /// Player move queues the directional move keys 
    /// to be processed along with the server.
    /// It also changes the sprite direction and 
    /// handles interaction with objects that can 
    /// be walked into it. 
    /// </summary>
    public class PlayerMove : NetworkBehaviour
    {
        private PlayerSprites playerSprites;
        private PlayerSync playerSync;
        [HideInInspector] public PushPull pushPull; //The push pull component attached to this player

        public KeyCode[] keyCodes = {KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.RightArrow};

        public bool diagonalMovement;
        public float speed = 10;
        [SyncVar] public bool allowInput = true;
        [SyncVar] public bool isGhost = false;
        private bool _isPush;

        public bool IsPushing
        {
            get { return _isPush; }
            set { _isPush = value; }
        }

        private Matrix _matrix;
        private Matrix matrix => _matrix ?? (_matrix = Matrix.GetMatrix(this));

        private List<KeyCode> pressedKeys = new List<KeyCode>();
        private bool _isMoving = false;

        void Start()
        {
            playerSprites = gameObject.GetComponent<PlayerSprites>();
            playerSync = GetComponent<PlayerSync>();
            pushPull = GetComponent<PushPull>();
        }

        /// temp solution for use with the UI network prediction
        public bool isMoving
        {
            get { return _isMoving; }
        }

        public PlayerAction SendAction()
        {
            var actionKeys = new List<int>();

            for (int i = 0; i < keyCodes.Length; i++)
            {
                if (PlayerManager.LocalPlayer == gameObject && UIManager.Chat.isChatFocus)
                    return new PlayerAction() {keyCodes = actionKeys.ToArray()};

                if (Input.GetKey(keyCodes[i]) && allowInput && !IsPushing)
                {
                    actionKeys.Add((int) keyCodes[i]);
                }
            }

            return new PlayerAction() {keyCodes = actionKeys.ToArray()};
        }

        public Vector3Int GetNextPosition(Vector3Int currentPosition, PlayerAction action)
        {
            var direction = GetDirection(action);
            if (!isGhost)
                playerSprites.FaceDirection(new Vector2(direction.x, direction.y));

            var adjustedDirection = AdjustDirection(currentPosition, direction);

            if (adjustedDirection == Vector3.zero)
            {
                Interact(currentPosition, direction);
            }

            return currentPosition + adjustedDirection;
        }

        private Vector3Int GetDirection(PlayerAction action)
        {
            ProcessAction(action);

            if (diagonalMovement)
            {
                return GetMoveDirection(pressedKeys);
            }
            if (pressedKeys.Count > 0)
            {
                return GetMoveDirection(pressedKeys[pressedKeys.Count - 1]);
            }
            return Vector3Int.zero;
        }

        private void ProcessAction(PlayerAction action)
        {
            var actionKeys = new List<int>(action.keyCodes);
            for (int i = 0; i < keyCodes.Length; i++)
            {
                if (actionKeys.Contains((int) keyCodes[i]) && !pressedKeys.Contains(keyCodes[i]))
                {
                    pressedKeys.Add(keyCodes[i]);
                }
                else if (!actionKeys.Contains((int) keyCodes[i]) && pressedKeys.Contains(keyCodes[i]))
                {
                    pressedKeys.Remove(keyCodes[i]);
                }
            }
        }

        private Vector3Int GetMoveDirection(List<KeyCode> actions)
        {
            var direction = Vector3Int.zero;
            for (int i = 0; i < pressedKeys.Count; i++)
            {
                direction += GetMoveDirection(pressedKeys[i]);
            }
            direction.x = Mathf.Clamp(direction.x, -1, 1);
            direction.y = Mathf.Clamp(direction.y, -1, 1);

            return direction;
        }

        private Vector3Int GetMoveDirection(KeyCode action)
        {
            if (PlayerManager.LocalPlayer == gameObject && UIManager.Chat.isChatFocus)
                return Vector3Int.zero;

            switch (action)
            {
                case KeyCode.W:
                case KeyCode.UpArrow:
                    return Vector3Int.up;
                case KeyCode.A:
                case KeyCode.LeftArrow:
                    return Vector3Int.left;
                case KeyCode.S:
                case KeyCode.DownArrow:
                    return Vector3Int.down;
                case KeyCode.D:
                case KeyCode.RightArrow:
                    return Vector3Int.right;
            }

            return Vector3Int.zero;
        }

        /// <summary>
        /// Check current and next tiles to determine their status and if movement is allowed
        /// </summary>
        private Vector3Int AdjustDirection(Vector3Int currentPosition, Vector3Int direction)
        {
            if (isGhost)
            {
                return direction;
            }

            //Is the current tile restrictive?
            var newPos = currentPosition + direction;

            if (!matrix.IsPassableAt(currentPosition, newPos))
            {
                return Vector3Int.zero;
            }

            if (matrix.IsPassableAt(newPos) || matrix.ContainsAt(newPos, gameObject))
            {
                return direction;
            }

            if (playerSync.pullingObject != null)
            {
                if (matrix.ContainsAt(newPos, playerSync.pullingObject))
                {
                    Vector2 directionToPullObj = playerSync.pullingObject.transform.localPosition - transform.localPosition;
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
            return Vector3Int.zero;
        }

        private void Interact(Vector3 currentPosition, Vector3 direction)
        {
            var position = Vector3Int.RoundToInt(currentPosition + direction);

            InteractDoor(currentPosition, direction);

            //Is the object pushable (iterate through all of the objects at the position):
            var pushPulls = matrix.Get<PushPull>(position).ToArray();
            for (int i = 0; i < pushPulls.Length; i++)
            {
                if (pushPulls[i] && pushPulls[i].gameObject != gameObject)
                {
                    pushPulls[i].TryPush(gameObject, speed, direction);
                }
            }
        }

        private void InteractDoor(Vector3 currentPosition, Vector3 direction)
        {
            var position = Vector3Int.RoundToInt(currentPosition + direction);

            var doorController = matrix.GetFirst<DoorController>(position);

            if (!doorController)
            {
                doorController = matrix.GetFirst<DoorController>(Vector3Int.RoundToInt(currentPosition));

                if (doorController)
                {
                    var registerDoor = doorController.GetComponent<RegisterDoor>();
                    if (registerDoor.IsPassable(position))
                    {
                        doorController = null;
                    }
                }
            }

            if (doorController != null && allowInput)
            {
                //checking if the door actually has a restriction (only need one because that's how ss13 works!
                if (doorController.restriction > 0)
                {
                    //checking if the ID slot on player contains an ID with an itemIdentity component
                    if (UIManager.InventorySlots.IDSlot.IsFull && UIManager.InventorySlots.IDSlot.Item.GetComponent<IDCard>() != null)
                    {
                        //checking if the ID has access to bypass the restriction
                        CheckDoorAccess(UIManager.InventorySlots.IDSlot.Item.GetComponent<IDCard>(), doorController);
                        //Check the current hand for an ID
                    }
                    else if (UIManager.Hands.CurrentSlot.IsFull && UIManager.Hands.CurrentSlot.Item.GetComponent<IDCard>() != null)
                    {
                        CheckDoorAccess(UIManager.Hands.CurrentSlot.Item.GetComponent<IDCard>(), doorController);
                    }
                    else
                    {
                        //does not have an ID
                        allowInput = false;
                        StartCoroutine(DoorInputCoolDown());
                        if (CustomNetworkManager.Instance._isServer)
                            doorController.CmdTryDenied();
                    }
                }
                else
                {
                    //door does not have restriction
                    allowInput = false;
                    //Server only here but it is a cmd for the input trigger (opening with mouse click from client)
                    if (CustomNetworkManager.Instance._isServer)
                        doorController.CmdTryOpen(gameObject);

                    StartCoroutine(DoorInputCoolDown());
                }
            }
        }

        void CheckDoorAccess(IDCard cardID, DoorController doorController)
        {
            if (cardID.accessSyncList.Contains((int) doorController.restriction))
            {
                // has access
                allowInput = false;
                //Server only here but it is a cmd for the input trigger (opening with mouse click from client)
                if (CustomNetworkManager.Instance._isServer)
                    doorController.CmdTryOpen(gameObject);

                StartCoroutine(DoorInputCoolDown());
            }
            else
            {
                // does not have access
                allowInput = false;
                StartCoroutine(DoorInputCoolDown());
                //Server only here but it is a cmd for the input trigger (opening with mouse click from client)
                if (CustomNetworkManager.Instance._isServer)
                    doorController.CmdTryDenied();
            }
        }

        //FIXME an ugly temp fix for an ugly problem. Will implement callbacks after 0.1.3
        IEnumerator DoorInputCoolDown()
        {
            yield return new WaitForSeconds(0.3f);
            allowInput = true;
        }
    }
}
