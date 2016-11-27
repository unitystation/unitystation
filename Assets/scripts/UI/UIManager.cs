using UnityEngine;
using System.Collections;
using SS.PlayGroup;

namespace UI{
public class UIManager : MonoBehaviour {

	public static UIManager control;

		//Child Scripts
		public ControlChat chatControl;
		public ControlBottomUI bottomControl;
		public HandSelector handSelector;
		public ControlIntent intentControl;
		public ControlAction actionControl;
		public ControlWalkRun walkRunControl;

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

		/// <summary>
		/// Is Oxygen On?
		/// </summary>
		public bool isOxygen{ get; set; }



	void Awake () {


		if (control == null) {
		
			control = this;
		
		} else {
		
			Destroy (this);
		
		}

	}

		void Start (){
			isRightHand = true;
		}

			

			
}
}