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
		public ControlIntent intentControl;
		public ControlAction actionControl;
		public ControlWalkRun walkRunControl;

		//sfx
		public AudioSource click01SFX;


		//Members accessable for player controller

		/// <summary>
		/// Current Intent status
		/// </summary>
		public Intent currentIntent{ get; set; }

		/// <summary>
		/// What is DamageZoneSeclector currently set at
		/// </summary>
		public DamageZoneSelector damageZone{ get; set; }

		/// <summary>
		/// Is right hand selected? otherwise it is left
		/// </summary>
		public bool isRightHand{ get; set; }

		/// <summary>
		/// Is throw selected?
		/// </summary>
		public bool isThrow{ get; set; }


	void Awake () {


		if (control == null) {
		
			control = this;
		
		} else {
		
			Destroy (this);
		
		}

	}
			
}
}