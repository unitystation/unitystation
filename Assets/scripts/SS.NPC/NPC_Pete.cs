using UnityEngine;
using System.Collections.Generic;
using MovementEffects;
using SS.PlayGroup;

namespace SS.NPC{
	
public class NPC_Pete : MonoBehaviour {

		private SpriteRenderer thisRend;
		private bool isRight = false;

		private PhysicsMove physicsMove;
		public float moveSpeed = 400f;


	void Start () {
			
			thisRend = GetComponent<SpriteRenderer> ();
			physicsMove = gameObject.AddComponent<PhysicsMove> ();
			physicsMove.moveSpeed = moveSpeed;

			Timing.RunCoroutine (RandMove (), "randmove");
	}
	
	

	
		void Flip(){

			Vector2 newScale = transform.localScale;
			newScale.x = -newScale.x;
			transform.localScale = newScale;


		}

		void OnDisable(){

			Timing.KillCoroutines ("randmove");

		}

		void OnCTriggerExit2D (Collider2D coll){

			if (coll.gameObject.layer == 8) {
				//player stopped pushing
				physicsMove.MoveInputReleased();

			}
		}

		//COROUTINES


		IEnumerator<float>RandMove(){

			float ranTime = Random.Range (5f, 15f);
			yield return Timing.WaitForSeconds (ranTime);

			int ranDir = Random.Range (0, 4);

			if (ranDir == 0) {
				//Move Up
				physicsMove.MoveInDirection(Vector2.up);
			

			} else if (ranDir == 1) {
				//Move Right
				physicsMove.MoveInDirection(Vector2.right);

				if(!isRight){

					isRight = true;
					Flip ();
				}

			} else if (ranDir == 2) {
				//Move Down
				physicsMove.MoveInDirection(Vector2.down);

			} else if (ranDir == 3) {
				//Move Left
				physicsMove.MoveInDirection(Vector2.left);
	
				if(isRight){

					isRight = false;
					Flip ();
				}

			}

			yield return Timing.WaitForSeconds (0.2f);
			physicsMove.MoveInputReleased ();

			Timing.RunCoroutine(RandMove (), "randmove");

		}
}
}