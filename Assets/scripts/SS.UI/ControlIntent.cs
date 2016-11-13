using UnityEngine;
using UnityEngine.UI;




namespace SS.UI{

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

			ControlUI.control.currentIntent = Intent.Help;
			thisImg = GetComponent<Image> ();
		}

		//OnClick method
		public void IntentButton(){
			PlayClick01 ();
			Debug.Log ("Intent Button");

			int curInt = (int)ControlUI.control.currentIntent;
			curInt++;

			if (curInt == 4)
				curInt = 0;
			
			ControlUI.control.currentIntent = (Intent)curInt;

			thisImg.sprite = sprites [curInt];

		

		}

		//SoundFX

		void PlayClick01(){

			if (ControlUI.control.click01SFX != null) {
				ControlUI.control.click01SFX.Play ();
			}

		}
}
}