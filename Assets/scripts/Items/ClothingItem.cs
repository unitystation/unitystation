using Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

namespace PlayGroup {
    [RequireComponent(typeof(SpriteRenderer))]
    public class ClothingItem: MonoBehaviour {
        public SlotType slotType;

        public string spriteSheetName;
        public int reference = -1;

        public int Reference {
            set {
                reference = value;
                UpdateSprite();
            }
            get {
                return reference;
            }
        }

        public Vector2 Direction {
            set {
                currentDirection = value;
                UpdateReferenceOffset();
            }
            get {
                return currentDirection;
            }
        }

        private SpriteRenderer spriteRenderer;
        private Sprite[] sprites;
        private int referenceOffset = 0;
        private Vector2 currentDirection = Vector2.down;


        void Start() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            sprites = SpriteManager.control.playerSprites[spriteSheetName];
            UpdateSprite();
        }

        public void Clear() {
            Reference = -1;
        }

        public void UpdateItem(GameObject item) {
            var attributes = item.GetComponent<ItemAttributes>();

            if(slotType == SlotType.Other) {
                reference = attributes.clothingReference;
            } else {
                switch(attributes.spriteType) {
                    case SpriteType.Items:
                        spriteSheetName = "items_";
                        break;
                    case SpriteType.Clothing:
                        spriteSheetName = "clothing_";
                        break;
                    case SpriteType.Guns:
                        spriteSheetName = "guns_";
                        break;
                }

                if(slotType == SlotType.RightHand) {
                    spriteSheetName += "righthand";
                    reference = attributes.inHandReferenceRight;
                } else {
                    spriteSheetName += "lefthand";
                    reference = attributes.inHandReferenceLeft;
                }

            }

            sprites = SpriteManager.control.playerSprites[spriteSheetName];
            UpdateSprite();
        }

        private void UpdateReferenceOffset() {

            if(currentDirection == Vector2.down)
                referenceOffset = 0;
            if(currentDirection == Vector2.up)
                referenceOffset = 1;
            if(currentDirection == Vector2.right)
                referenceOffset = 2;
            if(currentDirection == Vector2.left)
                referenceOffset = 3;

            UpdateSprite();
        }

        private void UpdateSprite() {
            if(spriteRenderer != null) {
                if(reference >= 0) {
                    spriteRenderer.sprite = sprites[reference + referenceOffset];
                } else if(spriteRenderer.sprite != null) {
                    spriteRenderer.sprite = null;
                }
            }
        }
    }
}