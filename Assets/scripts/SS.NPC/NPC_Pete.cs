using UnityEngine;
using System.Collections.Generic;
using MovementEffects;

namespace SS.NPC{
	
public class NPC_Pete : MonoBehaviour {

		private SpriteRenderer thisRend;
		private Rigidbody2D thisRigi;
		private bool isRight = false;


	void Start () {
			thisRend = GetComponent<SpriteRenderer> ();
			thisRigi = GetComponent<Rigidbody2D> ();


			Timing.RunCoroutine (RandMove (), "randmove");
	}
	
		void OnDisable(){

			Timing.KillCoroutines ("randmove");

		}

		IEnumerator<float>RandMove(){

			float ranTime = Random.Range (5f, 15f);
			yield return Timing.WaitForSeconds (ranTime);

			int ranDir = Random.Range (0, 4);

			if (ranDir == 0) {
				//Move Up
				Vector2 movePos = new Vector2 (transform.position.x, transform.position.y + 1f);
				thisRigi.MovePosition(movePos);
			
			} else if (ranDir == 1) {
				//Move Right
				Vector2 movePos = new Vector2 (transform.position.x + 1f, transform.position.y);
				thisRigi.MovePosition(movePos);

				if(!isRight){

					isRight = true;
					Flip ();
				}
			
			} else if (ranDir == 2) {
				//Move Down
				Vector2 movePos = new Vector2 (transform.position.x, transform.position.y - 1f);
				thisRigi.MovePosition(movePos);
			
			} else if (ranDir == 3) {
			//Move Left
				Vector2 movePos = new Vector2 (transform.position.x - 1f, transform.position.y);
				thisRigi.MovePosition(movePos);
				if(isRight){

					isRight = false;
					Flip ();
				}
			
			}

			Timing.RunCoroutine(RandMove (), "randmove");
		
		
		}
	
		void Flip(){

			Vector2 newScale = transform.localScale;
			newScale.x = -newScale.x;
			transform.localScale = newScale;


		}
}
}