using UnityEngine;
using UnityEngine.UI;


namespace UI{
public class OxygenButton : MonoBehaviour {

		public Sprite[] stateSprites;
		private Image thisImg;
	// Use this for initialization
	void Start () {
			thisImg = GetComponent<Image> ();
			UIManager.control.isOxygen = false;
	}
	
		public void OxygenSelect(){

			PlayClick01 ();
			if (!UIManager.control.isOxygen) {
			
				UIManager.control.isOxygen = true;
				thisImg.sprite = stateSprites [1];
			
			} else {
			
				UIManager.control.isOxygen = false;
				thisImg.sprite = stateSprites [0];
			
			}

		}

		//SoundFX

		void PlayClick01(){

			if (SoundManager.control != null) {
                SoundManager.control.Play("Click01");
			}

		}
}
}