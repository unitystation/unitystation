using UnityEngine;
using UnityEngine.UI;


namespace UI{

	public enum DamageZoneSelector{

		torso,
		head,
		eyes,
		mouth,
		r_arm,
		l_arm,
		r_leg,
		l_leg


	}

public class ZoneSelector : MonoBehaviour {

		public Sprite[] selectorSprites;
		public Image selImg;


		public void SelectAction(int curSelect){
			PlayClick01 ();
			selImg.sprite = selectorSprites [curSelect];
			UIManager.control.damageZone = (DamageZoneSelector)curSelect;

		}

		//SoundFX

		void PlayClick01(){

			if (SoundManager.control != null) {
				SoundManager.control.sounds["Click01"].Play ();
			}

		}


}
}