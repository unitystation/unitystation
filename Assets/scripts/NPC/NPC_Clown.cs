using UnityEngine;
using System.Collections;



namespace SS.NPC{
public class NPC_Clown : MonoBehaviour {

		private SpriteRenderer thisRend;
		public Sprite[] clownSprites;

		private bool isRight = false;

		void Start () {
			thisRend = GetComponent<SpriteRenderer> ();
			//Snap to grid
			//FIXME need to figure out the grid and how to round to it
			Vector2 newPos = new Vector2 (Mathf.Round (transform.position.x / 100f) *100f , Mathf.Round (transform.position.y / 100f)* 100f);
			transform.position = newPos;
			StartCoroutine (RandMove ());
		}

		void OnDisable(){

			StopCoroutine (RandMove());

		}

		IEnumerator RandMove(){

			float ranTime = Random.Range (0.2f,6f);

			yield return new WaitForSeconds (ranTime);

			int ranDir = Random.Range (0, 4);

			if (ranDir == 0) {
				//Move Up

				thisRend.sprite = clownSprites [2];
				Vector2 movePos = new Vector2 (transform.position.x, transform.position.y + 32f);
				transform.position = movePos;

			} else if (ranDir == 1) {
				//Move Right
				thisRend.sprite = clownSprites[3];
				Vector2 movePos = new Vector2 (transform.position.x + 32f, transform.position.y);
				transform.position = movePos;

				if(!isRight){

					isRight = true;
					Flip ();
				}

			} else if (ranDir == 2) {
				//Move Down
				thisRend.sprite = clownSprites [0];
				Vector2 movePos = new Vector2 (transform.position.x, transform.position.y - 32f);
				transform.position = movePos;

			} else if (ranDir == 3) {
				//Move Left
				thisRend.sprite = clownSprites[3];
				Vector2 movePos = new Vector2 (transform.position.x - 32f, transform.position.y);
				transform.position = movePos;
				if(isRight){

					isRight = false;
					Flip ();
				}

			
			}

			if (SoundManager.control != null) {
				float ranPitch = Random.Range (0.5f, 1.5f);
				SoundManager.control.Play ("ClownHonk", ranPitch);

			}
			StartCoroutine(RandMove ());


		}

		void Flip(){

			Vector2 newScale = transform.localScale;
			newScale.x = -newScale.x;
			transform.localScale = newScale;


		}

}
}