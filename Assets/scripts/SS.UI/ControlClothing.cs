using UnityEngine;
using System.Collections;

namespace SS.UI{
public class ControlClothing : MonoBehaviour {

		//EQUIP MEMBERS
		private bool EquipOut = false;
		public GameObject rollOutParent;

	// Use this for initialization
	void Start () {

			rollOutParent.SetActive (false);
	
	}
	
		//Button OnClick methods:
	public void EQUIP_RollOut(){
			PlayClick01 ();
		if (!rollOutParent.activeSelf) {
			rollOutParent.SetActive (true);
			EquipOut = true;

		} else {

			rollOutParent.SetActive (false);
			EquipOut = false;

		}
	}



		public void Shoes(){
			PlayClick01 ();
			Debug.Log ("Shoes Button");
		
		}

		public void Suit(){
			PlayClick01 ();
			Debug.Log ("Suit Button");

		}

		public void Armor(){
			PlayClick01 ();
			Debug.Log ("Armor Button");

		}

		public void Gloves(){
			PlayClick01 ();
			Debug.Log ("Gloves Button");

		}

		public void Neck(){
			PlayClick01 ();
			Debug.Log ("Neck Button");

		}

		public void Mask(){
			PlayClick01 ();
			Debug.Log ("Mask Button");

		}

		public void Ear(){
			PlayClick01 ();
			Debug.Log ("Ear Button");

		}

		public void Glasses(){
			PlayClick01 ();
			Debug.Log ("Glasses Button");

		}

		public void Hat(){
			PlayClick01 ();
			Debug.Log ("Hat Button");

		}


		//SoundFX

		void PlayClick01(){

			if (ControlUI.control.click01SFX != null) {
				ControlUI.control.click01SFX.Play ();
			}

		}




}
}