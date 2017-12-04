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
    public class ClothingItem : MonoBehaviour
    {
        //choice between left or right or other(clothing)
        public SpriteType spriteType;

        public string spriteSheetName;
        public int reference = -1;
        public PlayerScript thisPlayerScript;

        public int Reference
        {
            set
            {
                reference = value;
                SetSprite();
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
        }

        public void Clear()
        {
            Reference = -1;
        }

        void SetSprite()
        {

            if (reference == -1)
            {
                UpdateSprite();
                return;
            }

            if (spriteType == SpriteType.Other)
            {
                reference = Reference;
            }
            else
            {
                string networkRef = Reference.ToString();
                int code = (int)Char.GetNumericValue(networkRef[0]);
                networkRef = networkRef.Remove(0, 1);
                int _reference = int.Parse(networkRef);
                switch (code)
                {
                    case 1:
                        spriteSheetName = "items_";
                        break;
                    case 2:
                        spriteSheetName = "clothing_";
                        break;
                    case 3:
                        spriteSheetName = "guns_";
                        break;
                }
                if (spriteType == SpriteType.RightHand)
                {
                    spriteSheetName = spriteSheetName + "righthand";
                    reference = _reference;
                }
                else
                {
                    spriteSheetName = spriteSheetName + "lefthand";
                    reference = _reference;
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
                if (reference >= 0)
                { //If reference -1 then clear the sprite
                    if (sprites != null)
                        spriteRenderer.sprite = sprites[reference + referenceOffset];
                }
                else
                {
                    spriteRenderer.sprite = null;
                }
            }
        }
    }
}