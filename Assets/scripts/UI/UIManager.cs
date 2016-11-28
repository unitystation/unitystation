using UnityEngine;
using System.Collections;
using PlayGroup;

namespace UI{
public class UIManager : MonoBehaviour {

	public static UIManager control;

		//Child Scripts
		public ControlChat chatControl;
		public ControlBottomUI bottomControl;
		public Hands hands;
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


		public void ResetUI(){

			//This empties all slots and returns everything back to default values, or should
			//TODO: continue to update this when developing the UI. This is messy ATM

			if (hands.rightSlot.isFull) {
			
				Destroy (hands.rightSlot.inHandItem);
			}
			if (hands.leftSlot.isFull) {

				Destroy (hands.leftSlot.inHandItem);
			}
			if (bottomControl.storage01Slot.isFull) {
			
				Destroy (bottomControl.storage01Slot.inHandItem);
			
			}
			if (bottomControl.storage02Slot.isFull) {

				Destroy (bottomControl.storage02Slot.inHandItem);

			}


		}

			
}
}