using UnityEngine;
using System.Collections;
using Game;
using UI;
using System.Collections.Generic;

namespace PlayGroup {
    public class PlayerScript: MonoBehaviour {
        public bool isMine = false;
        //Is this controlled by the player or other players

        [HideInInspector]
        public PhysicsMove physicsMove;
        private PlayerSprites playerSprites;
        [HideInInspector]
        public PhotonView photonView;

        void Awake(){
            playerSprites = gameObject.GetComponent<PlayerSprites>();
            photonView = gameObject.GetComponent<PhotonView>();
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

			if (photonView.isMine) { //This prefab is yours, take control of it
				PlayerManager.control.SetPlayerForControl (this.gameObject);
			} else {
				BoxCollider2D boxColl = gameObject.GetComponent<BoxCollider2D> ();
				boxColl.isTrigger = true;
			}
			if (PhotonNetwork.connectedAndReady) {
				gameObject.name = photonView.owner.NickName;
			}
        }

        //THIS IS ONLY USED FOR LOCAL PLAYER
        public void MovePlayer(Vector2 direction) {
            //At the moment it just checks if the input window is open and if it is false then allow move
            if(!UIManager.control.chatControl.chatInputWindow.activeSelf && isMine) {
                physicsMove.MoveInDirection(direction); //Tile based physics move
                playerSprites.FaceDirection(direction); //Handles the playersprite change on direction change

            }
        }

        public float DistanceTo(Vector3 position) {
            return (transform.position - position).magnitude;
        }

		//Will turn this into a PlayerCollisions components after developing the methods

		void OnTriggerEnter2D(Collider2D coll){
		
			if (coll.gameObject.layer == 8) {
			
				Debug.Log (gameObject.name + " Collided with " + coll.gameObject.name);
			
			}
		
		}
    }
}
