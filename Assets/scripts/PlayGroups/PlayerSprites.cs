using UnityEngine;
using System.Collections;
using UI;
using System.Collections.Generic;

namespace PlayGroup
{
    public class PlayerSprites: Photon.PunBehaviour
    {
		[HideInInspector]
        public Vector2 currentDirection = Vector2.down;
        private PlayerScript playerScript;
        [HideInInspector]
        public  PhotonView photonView;

        private Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

        void Awake()
        {
            foreach (var c in GetComponentsInChildren<ClothingItem>())
            {
                clothes[c.name] = c;
            }
            FaceDirection(Vector2.down);
            photonView = gameObject.GetComponent<PhotonView>();
            playerScript = gameObject.GetComponent<PlayerScript>();
        }

        void Start(){

            if (PhotonNetwork.connectedAndReady)
            {
                if(!photonView.isMine && !PhotonNetwork.isMasterClient){
                StartCoroutine(WaitToSync()); //Give it a chance to get all of the clothitems
                }
            }
        }
        //turning character input and sprite update
        public void FaceDirection(Vector2 direction)
        {
            if (playerScript != null)
            {
				if (PhotonNetwork.connectedAndReady) {
					if (playerScript.isMine) {//if this player is mine, then update your dir on all other clients
						photonView.RPC ("UpdateDirection", PhotonTargets.Others, new object[] { direction });
						SetDir (direction);
					} else {
						SetDir (direction); 
					}
				} else {
					SetDir (direction); //dev mode
				}
            }
        }

        void SetDir(Vector2 direction)
        {
            if (currentDirection != direction)
            {
                foreach (var c in clothes.Values)
                {
                    c.Direction = direction;
                }

                currentDirection = direction;
            }
        }

        public void PickedUpItem(GameObject item)
        {
            if (UIManager.control.isRightHand)
            {
                clothes["rightHand"].UpdateItem(item);
            }
            else
            {
                clothes["leftHand"].UpdateItem(item);
            }
        }

        public void RemoveItemFromHand(bool rightHand)
        {
            if (rightHand)
            {
                clothes["rightHand"].Clear();
            }
            else
            {
                clothes["leftHand"].Clear();
            }
        }

        [PunRPC]
        void UpdateDirection(Vector2 dir)
        {
            FaceDirection(dir);
            
        }

        //PUN Sync
        [PunRPC]
        void SendCurrentState()
        {
            if (PhotonNetwork.isMasterClient)
            {
                Debug.Log("SENDING CURRENT STATE");
                photonView.RPC("UpdateDirection", PhotonTargets.Others, new object[] { currentDirection });
            }
        }
            
        IEnumerator WaitToSync(){

            yield return new WaitForSeconds(0.2f);
                //If you are not the master then update the current IG state of this object from the master
                photonView.RPC("SendCurrentState", PhotonTargets.MasterClient, null);
           
        }
    }
}