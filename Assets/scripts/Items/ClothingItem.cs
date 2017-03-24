using Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
    public enum SpriteType
    {
        Other, RightHand, LeftHand
    }

    [RequireComponent(typeof(SpriteRenderer))]
	public class ClothingItem: MonoBehaviour
    {
        public SpriteType spriteType;

        public string spriteSheetName;
        public int reference = -1;
        public PlayerScript thisPlayerScript;

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
            sprites = SpriteManager.PlayerSprites[spriteSheetName];
            UpdateSprite();
		
			if (!thisPlayerScript.isServer && !thisPlayerScript.isLocalPlayer) {
				//If you are not the server then update the current IG state of this object from the server
				CmdSendCurrentState();
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

            sprites = SpriteManager.PlayerSprites[spriteSheetName];
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
					if(sprites != null)
                    spriteRenderer.sprite = sprites[reference + referenceOffset];
                }
                else
                {
                    spriteRenderer.sprite = null;
                }
            }
      
            if (thisPlayerScript != null)
            {
				if (thisPlayerScript.isLocalPlayer)//if this player is mine, then update the reference and spriteSheetName on all other clients
                {
//					CmdUpdateSpriteNetwork(reference, spriteSheetName, thisPlayerScript.netId);
					Debug.Log("FIXME: handle all clothing changes on root object as NetworkIdentities not allowed on children");
                }
            }
        }

		//FIXME: Cannot use these server and client RPC's as uNet does not allowe NetworkIdentities on children. Handle through root object
//        [Command]
		void CmdUpdateSpriteNetwork(int spriteRef, string sheetName, NetworkInstanceId id)
        {
                spriteSheetName = sheetName;
                sprites = SpriteManager.PlayerSprites[spriteSheetName];
                Reference = spriteRef;
				RpcUpdateClientSprites(spriteRef, sheetName, id);
        }

//		[ClientRpc]
		void RpcUpdateClientSprites(int spriteRef, string sheetName, NetworkInstanceId id){
			if (thisPlayerScript.netId != id) {
				spriteSheetName = sheetName;
				sprites = SpriteManager.PlayerSprites[spriteSheetName];
				Reference = spriteRef;
			}
		}

        //Update the clothing item from the server if this object isn't yours
//        [Command]
        void CmdSendCurrentState()
        {
			RpcReceiveCurrentState(reference); // Send the clothing reference
        }

//		[ClientRpc]
        void RpcReceiveCurrentState(int clothRef)
        {
			if (!thisPlayerScript.isLocalPlayer)
            {
                Reference = clothRef;
            }
        }
            
    }
}