using UnityEngine;
using System.Collections;

public class ControlUI : MonoBehaviour {

	public static ControlUI control;


	//EQUIP MEMBERS
	private bool EquipOut = false;
	public GameObject rollOutParent;




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


	/// <summary>
	/// OnClick from Bag Button (bottom left) triggers rollout
	/// </summary>
	public void EQUIP_RollOut(){
		
		if (!rollOutParent.activeSelf) {
			rollOutParent.SetActive (true);
			EquipOut = true;
		
		} else {
		
			rollOutParent.SetActive (false);
			EquipOut = false;
		
		}
		}

}
