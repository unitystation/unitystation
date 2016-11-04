using UnityEngine;
using UnityEngine.UI;


namespace SS.UI{
public class OxygenButton : MonoBehaviour {

		public Sprite[] stateSprites;
		private Image thisImg;
	// Use this for initialization
	void Start () {
			thisImg = GetComponent<Image> ();
			ControlUI.control.isOxygen = false;
	}
	
		public void OxygenSelect(){

			PlayClick01 ();
			if (!ControlUI.control.isOxygen) {
			
				ControlUI.control.isOxygen = true;
				thisImg.sprite = stateSprites [1];
			
			} else {
			
				ControlUI.control.isOxygen = false;
				thisImg.sprite = stateSprites [0];
			
			}

		}

		//SoundFX

		void PlayClick01(){

			if (ControlUI.control.click01SFX != null) {
				ControlUI.control.click01SFX.Play ();
			}

		}
}
}