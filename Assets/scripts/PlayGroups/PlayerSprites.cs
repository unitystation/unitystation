using UnityEngine;
using System.Collections;
using UI;
using System.Collections.Generic;

namespace PlayGroup
{
    public class PlayerSprites: MonoBehaviour
    {
        private Vector2 currentDirection = Vector2.down;
        public PlayerScript playerScript;
        public  PhotonView photonView;

        private Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

        void Awake()
        {
            foreach (var c in GetComponentsInChildren<ClothingItem>())
            {
                clothes[c.name] = c;
            }
            FaceDirection(Vector2.down);
        }

        //turning character input and sprite update
        public void FaceDirection(Vector2 direction)
        {
            if (playerScript != null)
            {
                if (PhotonNetwork.connectedAndReady && playerScript.isMine) //if this player is mine, then update your dir on all other clients
                {
                    CallRemoteMethod(direction);
                }
            }
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

        //Photon RPC
        public void CallRemoteMethod(Vector2 dir)
        {
            if (photonView != null)
            {
                photonView.RPC(
                    "UpdateDirection",
                    PhotonTargets.Others, //Called on all clients for this PhotonView ID
                    new object[] { dir });

            }
        }

        [PunRPC]
        void UpdateDirection(Vector2 dir)
        {
            FaceDirection(dir);
        }
    }
}