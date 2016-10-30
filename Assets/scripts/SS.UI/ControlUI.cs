using UnityEngine;
using System.Collections;

namespace SS.UI{
public class ControlUI : MonoBehaviour {

	public static ControlUI control;

		//Bools

		//Hand selector public member
		public bool isRightHand;

	//EQUIP MEMBERS
	private bool EquipOut = false;
	public GameObject rollOutParent;

		//sfx
		public AudioSource click01SFX;


	void Awake () {


		if (control == null) {
		
			control = this;
		
		} else {
		
			Destroy (this);
		
		}

	}


	void Start () {

		rollOutParent.SetActive (false);
	}
			
	/// Clothing Equip roll out
	public void EQUIP_RollOut(){

			//soundFX trial placement here
			click01SFX.Play ();
		if (!rollOutParent.activeSelf) {
			rollOutParent.SetActive (true);
			EquipOut = true;
		
		} else {
		
			rollOutParent.SetActive (false);
			EquipOut = false;
		
		}
		}

}
}