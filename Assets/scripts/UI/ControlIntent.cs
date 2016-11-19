using UnityEngine;
using UnityEngine.UI;




namespace UI{

	public enum Intent{

		Help,
		Disarm,
		Hold,
		Attack

	}

public class ControlIntent : MonoBehaviour {


		public Sprite[] sprites;
		private Image thisImg;

	


		void Start(){

			UIManager.control.currentIntent = Intent.Help;
			thisImg = GetComponent<Image> ();
		}

		//OnClick method
		public void IntentButton(){
			PlayClick01 ();
			Debug.Log ("Intent Button");

			int curInt = (int)UIManager.control.currentIntent;
			curInt++;

			if (curInt == 4)
				curInt = 0;
			
			UIManager.control.currentIntent = (Intent)curInt;

			thisImg.sprite = sprites [curInt];

		

		}

		//SoundFX

		void PlayClick01(){

			if (SoundManager.control != null) {
				SoundManager.control.sounds[5].Play ();
			}

		}
}
}