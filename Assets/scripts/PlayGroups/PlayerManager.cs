using UnityEngine;
using UnityEngine.EventSystems;
using UI;
using Network;

//Handles control and spawn of player prefab
namespace PlayGroup {
    public class PlayerManager: Photon.PunBehaviour, IPunObservable {
        public float Health = 100f;        
        public Transform playerPrefab;        
        public Transform spawnPoint;

        public static GameObject LocalPlayer { get; private set; }
        public static Equipment Equipment { get; private set; }        
        public static PlayerScript LocalPlayerScript { get; private set; }
        
        //For access via other parts of the game
        public static PlayerScript PlayerScript { get; private set; }
        public static bool HasSpawned { get; private set; }

        //True, when the user is firing
        bool IsFiring;

        private static PlayerManager playerManager;

        public static PlayerManager Instance {
            get {
                if(!playerManager) {
                    playerManager = FindObjectOfType<PlayerManager>();
                }

                return playerManager;
            }
        }
            
        public static void Reset() {
            HasSpawned = false;
        }

        public static void SetPlayerForControl(GameObject playerObjToControl) {
            LocalPlayer = playerObjToControl;
            LocalPlayerScript = playerObjToControl.GetComponent<PlayerScript>();
            LocalPlayerScript.IsMine = true; // Set this object to yours, the rest are for network players

            PlayerScript = LocalPlayerScript; // Set this on the manager so it can be accessed by other components/managers
            Camera2DFollow.followControl.target = LocalPlayer.transform;

			Equipment = Instance.GetComponent<Equipment>();
			Equipment.enabled = true;
        }
			
        //CHECK HERE FOR AN EXAMPLE OF INSTANTIATING ITEMS ON PHOTON
        public static void CheckIfSpawned() {
            Debug.Log("CHECK IF SPAWNED");

            if(!HasSpawned) {
                if(GameData.IsInGame && NetworkManager.IsConnected) {
                    PhotonNetwork.Instantiate(Instance.playerPrefab.name, Instance.spawnPoint.position, Quaternion.identity, 0); //TODO: More spawn points and a way to iterate through them
                    HasSpawned = true;
                }
            }
        }

        public void Update() {
            // only process controls if local player exists
            if(HasSpawned && LocalPlayerScript != null) {
                this.ProcessInputs();

                if(this.Health <= 0f) {
                    //TODO DEATH
                }

            }
        }

        public static bool PlayerInReach(Transform transform) {
			if (PlayerScript != null) {
				return PlayerScript.IsInReach(transform);
			} else {
				return false;
			}
        }

        /// <summary>
        /// Processes the inputs. This MUST ONLY BE USED when the player has authority over this Networked GameObject (photonView.isMine == true)
        /// </summary>
        void ProcessInputs() {
            //INPUT CONTROLS HERE
            if(Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.D)) {
                LocalPlayerScript.physicsMove.MoveInputReleased();
            }

            Vector2 direction = Vector2.zero;

            //hold key down inputs. clampPos is used to snap player to an axis on movement
            if(Input.GetKey(KeyCode.D) && !LocalPlayerScript.physicsMove.isMoving || Input.GetKey(KeyCode.D) && LocalPlayerScript.physicsMove.isMoving && LocalPlayerScript.physicsMove.MoveDirection == Vector2.left) {
                //RIGHT
                direction = Vector2.right;

            }
            if(Input.GetKey(KeyCode.A) && !LocalPlayerScript.physicsMove.isMoving || Input.GetKey(KeyCode.A) && LocalPlayerScript.physicsMove.isMoving && LocalPlayerScript.physicsMove.MoveDirection == Vector2.right) {
                //LEFT
                direction = Vector2.left;

            }
            if(Input.GetKey(KeyCode.S) && !LocalPlayerScript.physicsMove.isMoving || Input.GetKey(KeyCode.S) && LocalPlayerScript.physicsMove.isMoving && LocalPlayerScript.physicsMove.MoveDirection == Vector2.up) {
                //DOWN
                direction = Vector2.down;

            }
            if(Input.GetKey(KeyCode.W) && !LocalPlayerScript.physicsMove.isMoving || Input.GetKey(KeyCode.W) && LocalPlayerScript.physicsMove.isMoving && LocalPlayerScript.physicsMove.MoveDirection == Vector2.down) {

                direction = Vector2.up;
            }

            if(direction != Vector2.zero) {
                LocalPlayerScript.MovePlayer(direction);
            }
        }

        // IPunObservable implementation
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
            if(stream.isWriting) {
                // We own this player: send the others our data
                stream.SendNext(this.IsFiring);
                stream.SendNext(this.Health);
            } else {
                // Network player, receive data
                this.IsFiring = (bool) stream.ReceiveNext();
                this.Health = (float) stream.ReceiveNext();
            }
        }
    }
}
