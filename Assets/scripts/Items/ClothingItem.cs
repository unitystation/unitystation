using Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

namespace PlayGroup
{
    public enum SpriteType
    {
        Other,
        RightHand,
        LeftHand
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class ClothingItem: Photon.PunBehaviour
    {
        public SpriteType spriteType;

        public string spriteSheetName;
        public int reference = -1;
        public PlayerScript thisPlayerScript;
        public PhotonView photonView;

        public int Reference
        {
            set
            {
                reference = value;
                UpdateSprite();
            }
            get
            {
                return reference;
            }
        }

        public Vector2 Direction
        {
            set
            {
                currentDirection = value;
                UpdateReferenceOffset();
            }
            get
            {
                return currentDirection;
            }
        }

        public SpriteRenderer spriteRenderer;
        private Sprite[] sprites;
        private int referenceOffset = 0;
        private Vector2 currentDirection = Vector2.down;

        void Start()
        {
            sprites = SpriteManager.control.playerSprites[spriteSheetName];
            UpdateSprite();

            if (PhotonNetwork.connectedAndReady)
            {
                if (!PhotonNetwork.isMasterClient && !photonView.isMine)
                {
                    //If you are not the master then update the current IG state of this object from the master
                    photonView.RPC("SendCurrentState", PhotonTargets.MasterClient);
                }
            }
        }

        public void Clear()
        {
            Reference = -1;
        }

        public void UpdateItem(GameObject item)
        {
            var attributes = item.GetComponent<ItemAttributes>();

            if (spriteType == SpriteType.Other)
            {
                reference = attributes.clothingReference;
            }
            else
            {
                switch (attributes.spriteType)
                {
                    case UI.SpriteType.Items:
                        spriteSheetName = "items_";
                        break;
                    case UI.SpriteType.Clothing:
                        spriteSheetName = "clothing_";
                        break;
                    case UI.SpriteType.Guns:
                        spriteSheetName = "guns_";
                        break;
                }

                if (spriteType == SpriteType.RightHand)
                {
                    spriteSheetName += "righthand";
                    reference = attributes.inHandReferenceRight;
                }
                else
                {
                    spriteSheetName += "lefthand";
                    reference = attributes.inHandReferenceLeft;
                }

            }

            sprites = SpriteManager.control.playerSprites[spriteSheetName];
            UpdateSprite();
        }

        private void UpdateReferenceOffset()
        {

            if (currentDirection == Vector2.down)
                referenceOffset = 0;
            if (currentDirection == Vector2.up)
                referenceOffset = 1;
            if (currentDirection == Vector2.right)
                referenceOffset = 2;
            if (currentDirection == Vector2.left)
                referenceOffset = 3;

            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (spriteRenderer != null)
            {
                if (reference >= 0) //If reference -1 then clear the sprite
                {
                    spriteRenderer.sprite = sprites[reference + referenceOffset];
                }
                else
                {
                    spriteRenderer.sprite = null;
                }
            }
      
            if (thisPlayerScript != null)
            {
                if (PhotonNetwork.connectedAndReady && photonView.isMine)//if this player is mine, then update the reference and spriteSheetName on all other clients
                {
                    photonView.RPC("UpdateSpriteNetwork", PhotonTargets.Others, new object[] { reference, spriteSheetName, photonView.viewID });
                }
            }
        }


        [PunRPC]
        void UpdateSpriteNetwork(int spriteRef, string sheetName, int viewID)
        {
            if (viewID == photonView.viewID)
            {
                spriteSheetName = sheetName;
                sprites = SpriteManager.control.playerSprites[spriteSheetName];
                Reference = spriteRef;
            }
        }

        //PUN Sync
        [PunRPC]
        void SendCurrentState()
        {
            if (PhotonNetwork.isMasterClient)
            {
                photonView.RPC("ReceiveCurrentState", PhotonTargets.Others, new object[]{reference}); // Send the clothing reference
            }
        }

        [PunRPC]
        void ReceiveCurrentState(int clothRef)
        {
            if (!photonView.isMine)
            {
                Reference = clothRef;
            }
        }
            
    }
}