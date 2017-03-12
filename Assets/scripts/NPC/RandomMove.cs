﻿using UnityEngine;
using System.Collections;
using PlayGroup;

namespace SS.NPC {

    public class RandomMove: MonoBehaviour {
        private PhysicsMove physicsMove;
        private SpriteRenderer spriteRenderer;
        private bool isRight = false;
        private bool isMoving = false;


        void Start() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            physicsMove = gameObject.GetComponent<PhysicsMove>();
            StartCoroutine(RandMove());
        }

        void Flip() {
            Vector2 newScale = transform.localScale;
            newScale.x = -newScale.x;
            transform.localScale = newScale;
        }

        void OnDisable() {
            StopCoroutine(RandMove());
        }

        void OnCTriggerExit2D(Collider2D coll) {
            if(coll.gameObject.layer == 8) {
                //player stopped pushing
                physicsMove.MoveInputReleased();
            }
        }

        void OnTriggerExit2D(Collider2D coll) {
            //Players layer
            if(coll.gameObject.layer == 8) {
                physicsMove.ForceSnapToTile();
            }
        }

        void Update() {
            if(!isMoving)
                StartCoroutine(RandMove());
        }

        //COROUTINES
        IEnumerator RandMove() {
            isMoving = true;
            physicsMove.ForceSnapToTile();
            float ranTime = Random.Range(5f, 15f);
            yield return new WaitForSeconds(ranTime);

            int ranDir = Random.Range(0, 4);

            if(ranDir == 0) {
                //Move Up
                physicsMove.MoveInDirection(Vector2.up);


            } else if(ranDir == 1) {
                //Move Right
                physicsMove.MoveInDirection(Vector2.right);

                if(!isRight) {
                    isRight = true;
                    Flip();
                }

            } else if(ranDir == 2) {
                //Move Down
                physicsMove.MoveInDirection(Vector2.down);

            } else if(ranDir == 3) {
                //Move Left
                physicsMove.MoveInDirection(Vector2.left);

                if(isRight) {
                    isRight = false;
                    Flip();
                }
            }

            yield return new WaitForSeconds(0.2f);
            physicsMove.MoveInputReleased();
            isMoving = false;
        }
    }
}