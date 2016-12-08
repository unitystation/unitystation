using Sprites;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayGroup {
    [RequireComponent(typeof(SpriteRenderer))]
    public class ClothingItem: MonoBehaviour {
        public string spriteSheetName;
        
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
        public int reference = -1;
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
                }else if(spriteRenderer.sprite != null) {
                    spriteRenderer.sprite = null;
                }
            }
        }
    }
}