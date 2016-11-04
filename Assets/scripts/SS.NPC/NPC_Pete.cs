using UnityEngine;
using System.Collections.Generic;
using MovementEffects;

namespace SS.NPC{
	
public class NPC_Pete : MonoBehaviour {

		private SpriteRenderer thisRend;
		private bool isRight = false;


	void Start () {
			thisRend = GetComponent<SpriteRenderer> ();
			//Snap to grid
			//FIXME need to figure out the grid and how to round to it
			Vector2 newPos = new Vector2 (Mathf.Round (transform.position.x / 100f) *100f , Mathf.Round (transform.position.y / 100f)* 100f);
			transform.position = newPos;
			Timing.RunCoroutine (RandMove (), "randmove");
	}
	
		void OnDisable(){

			Timing.KillCoroutines ("randmove");

		}

		IEnumerator<float>RandMove(){

			float ranTime = Random.Range (5f, 15f);
			Debug.Log (ranTime);
			yield return Timing.WaitForSeconds (ranTime);

			int ranDir = Random.Range (0, 4);

			if (ranDir == 0) {
				//Move Up
				Vector2 movePos = new Vector2 (transform.position.x, transform.position.y + 32f);
				transform.position = movePos;
			
			} else if (ranDir == 1) {
				//Move Right
				Vector2 movePos = new Vector2 (transform.position.x + 32f, transform.position.y);
				transform.position = movePos;

				if(!isRight){

					isRight = true;
					Flip ();
				}
			
			} else if (ranDir == 2) {
				//Move Down
				Vector2 movePos = new Vector2 (transform.position.x, transform.position.y - 32f);
				transform.position = movePos;
			
			} else if (ranDir == 3) {
			//Move Left
				Vector2 movePos = new Vector2 (transform.position.x - 32f, transform.position.y);
				transform.position = movePos;
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