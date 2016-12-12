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
        public PlayerSprites playerSprites;
        public PhotonView photonView;

        [Header("Temp Player Preferences - start sprites (facingDown)")]
        public int bodyNumber;
        public int suitNumber;
        public int beltNumber;
        public int headNumber;
        public int shoesNumber;
        public int underWearNumber;
        public int uniformNumber;

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

            SetPlayerPrefs();

            if(photonView.isMine) { //This prefab is yours, take control of it
                PlayerManager.control.SetPlayerForControl(this.gameObject);
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

        //Temp
        void SetPlayerPrefs() {
            var prefs = new Dictionary<string, int>();
            prefs["human"] = bodyNumber;
            prefs["suit"] = suitNumber;
            prefs["belt"] = beltNumber;
            prefs["head"] = headNumber;
            prefs["feet"] = shoesNumber;
            //prefs["face"] = shoesNumber; TODO
            //prefs["mask"] = shoesNumber;
            prefs["underwear"] = underWearNumber;
            prefs["uniform"] = uniformNumber;

            playerSprites.SetSprites(prefs);
        }

        public float DistanceTo(Vector3 position) {
            return (transform.position - position).magnitude;
        }
    }
}
