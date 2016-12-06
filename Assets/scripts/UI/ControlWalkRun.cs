using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI{
public class ControlWalkRun : MonoBehaviour {


		public Sprite[] runWalkSprites;
		private Image thisImg;

		public bool isRun{ get; set; }



		void Start(){

			thisImg = GetComponent<Image> ();

		}

		/* 
		 * Button OnClick methods
		 */

	public void RunWalk(){
		PlayClick01 ();
		Debug.Log ("RunWalk Button");

			if (!isRun) {
			
				isRun = true;
				thisImg.sprite = runWalkSprites [1];
			
			} else {
			
				isRun = false;
				thisImg.sprite = runWalkSprites [0];

			}

	}

	//SoundFX

		void PlayClick01(){

            if (SoundManager.control != null) {
				SoundManager.control.sounds["Click01"].Play ();
			}

		}
}
}