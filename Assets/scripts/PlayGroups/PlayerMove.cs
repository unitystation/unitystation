using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup {

    public class PlayerMove: NetworkBehaviour {
        [Header("Options")]
        public float speed = 10f;
        public bool allowDiagonalMove;

        private Vector3 currentPosition, targetPosition, currentDirection;
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
                currentPosition = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));

                var inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0f);

                if(inputDirection != Vector3.zero) {

                    if(!allowDiagonalMove && inputDirection.x != 0) {
                        inputDirection.y = 0;
                    }

                    if(!TryToMove(inputDirection)) {
                        Interact(inputDirection);
                        playerSprites.FaceDirection(inputDirection);
                    } else {
                        playerSprites.FaceDirection(inputDirection);
                    }
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