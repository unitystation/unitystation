using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup {

    public class PlayerMove: NetworkBehaviour {
        [Header("Options")]
        public float speed = 10f;
        public bool allowDiagonalMove;

        private Vector3 currentPosition, targetPosition, currentDirection, inputDirection;
        private PlayerSprites playerSprites;

        void Start() {
            targetPosition = transform.position;
            playerSprites = gameObject.GetComponent<PlayerSprites>();
        }

        void Update() {
            if(!UIManager.Chat.chatInputWindow.activeSelf)
                Move();
        }

        void Move() {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, Time.deltaTime * speed);

            if(targetPosition == transform.position) {
                currentPosition = new Vector3(Mathf.Round(transform.position.x),
                    Mathf.Round(transform.position.y));

                var newInputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0f);

                if(newInputDirection != Vector3.zero) {

                    var moveDirection = currentDirection;

                    if(inputDirection != newInputDirection) {
                        if(!allowDiagonalMove) {
                            if(newInputDirection.x != 0 && newInputDirection.y != 0) {
                                moveDirection = newInputDirection - inputDirection;
                            } else {
                                if(newInputDirection.x != 0) newInputDirection.y = 0;
                                moveDirection = newInputDirection;
                            }
                        } else {
                            moveDirection = newInputDirection;
                        }
                    }

                    if(!TryToMove(moveDirection)) {
                        Interact(moveDirection);
                        playerSprites.FaceDirection(moveDirection);
                    } else {
                        playerSprites.FaceDirection(targetPosition - currentPosition);
                    }
                    currentDirection = moveDirection;
                    inputDirection = newInputDirection;
                }

            }
        }

        private bool TryToMove(Vector3 direction) {
            var horizontal = Vector3.Scale(direction, Vector3.right);
            var vertical = Vector3.Scale(direction, Vector3.up);

            if(Matrix.Matrix.At(currentPosition + direction).IsPassable()) {
                if((Matrix.Matrix.At(currentPosition + horizontal).IsPassable() ||
                   Matrix.Matrix.At(currentPosition + vertical).IsPassable())) {

                    targetPosition = currentPosition + direction;
                    return true;
                }
            } else if(horizontal != Vector3.zero && vertical != Vector3.zero) {
                if(Matrix.Matrix.At(currentPosition + horizontal).IsPassable()) {
                    targetPosition = currentPosition + horizontal;
                    return true;
                } else if(Matrix.Matrix.At(currentPosition + vertical).IsPassable()) {
                    targetPosition = currentPosition + vertical;
                    return true;
                }
            }

            return false;
        }

        private void Interact(Vector3 direction) {
            DoorController doorController = Matrix.Matrix.At(currentPosition + direction).GetDoor();
            if(doorController != null) {
                doorController.CmdTryOpen();
            }
        }
    }
}