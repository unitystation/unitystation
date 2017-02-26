using UnityEngine;
using System.Collections;
using UI;

namespace PlayGroup {
    public class PlayerScript: MonoBehaviour {
        // the maximum distance the player needs to be to an object to interact with it
        public float interactionDistance = 2f;

        public bool IsMine { get; set; }
        //Is this controlled by the player or other players

        [HideInInspector]
        public PhysicsMove physicsMove;
        [HideInInspector]
        public PlayerSprites playerSprites;
        private PlayerCollisions playerCollisions;
        [HideInInspector]
        public PhotonView photonView;

        void Awake() {
            playerSprites = gameObject.GetComponent<PlayerSprites>();
            photonView = gameObject.GetComponent<PhotonView>();
            playerCollisions = gameObject.AddComponent<PlayerCollisions>();
        }

        void Start() {
            GameObject searchPlayerList = GameObject.FindGameObjectWithTag("PlayerList");
            if(searchPlayerList != null) {
                transform.parent = searchPlayerList.transform;
            } else {
                Debug.LogError("Scene is missing PlayerList GameObject!!");

            }
            //add physics move component and set default movespeed
            physicsMove = gameObject.GetComponent<PhysicsMove>();

            //Add player sprite controller component

            if(photonView.isMine) { //This prefab is yours, take control of it
				StartCoroutine("WaitForMapLoad");
            } else {
                BoxCollider2D boxColl = gameObject.GetComponent<BoxCollider2D>();
                boxColl.isTrigger = true;
            }
            if(PhotonNetwork.connectedAndReady) {
                gameObject.name = photonView.owner.NickName;
            }
        }

		//This fixes the bug of master client setting equipment before the UI is read (because it is the one that loads the map)
		IEnumerator WaitForMapLoad(){
			yield return new WaitForSeconds(1f);
			PlayerManager.SetPlayerForControl(this.gameObject);
		}

        //THIS IS ONLY USED FOR LOCAL PLAYER
        public void MovePlayer(Vector2 direction) {
            //At the moment it just checks if the input window is open and if it is false then allow move
            if(!UIManager.Chat.chatInputWindow.activeSelf && IsMine) {
                physicsMove.MoveInDirection(direction); //Tile based physics move
                playerSprites.FaceDirection(direction); //Handles the playersprite change on direction change

            }
        }

        public float DistanceTo(Vector3 position) {
            return (transform.position - position).magnitude;
        }

        public bool IsInReach(Transform transform) {
            return DistanceTo(transform.position) <= interactionDistance;
        }

		public bool IsInReachOfSwitch(Transform _transform, SwitchDirection switchDir) {
			if(DistanceTo(_transform.position) <= interactionDistance){
				if (switchDir == SwitchDirection.up && transform.position.y > _transform.position.y) {
					return true;
				} else if (switchDir == SwitchDirection.down && transform.position.y < _transform.position.y) {
					return true;
				} else if (switchDir == SwitchDirection.right && transform.position.x < _transform.position.x) {
					return true;
				} else if (switchDir == SwitchDirection.left && transform.position.x < _transform.position.x) {
					return true;
				} else {
					return false;
				}
			} else {
				return false;
			}
		}
    }
}
