using UnityEngine;
using System.Collections;

namespace SS.UI{
public class ControlUI : MonoBehaviour {

	public static ControlUI control;

		/* This static class is intended to be
		 * the 'go-to' class for anything UI
		 * related. Public members + access to child
		 * functions
		 */

		//Child Scripts
		public ControlChat chatControl;
		public ControlBottomUI bottomControl;
		public HandSelector handSelector;
		public ControlIntent ControlIntent;

		//sfx
		public AudioSource click01SFX;


		/// <summary>
		/// Is right hand selected? otherwise it is left
		/// </summary>
		public bool isRightHand;



	void Awake () {


		if (control == null) {
		
			control = this;
		
		} else {
		
			Destroy (this);
		
		}

	}
			
}
}