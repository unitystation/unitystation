using UnityEngine;
using System.Collections;
using SS.PlayGroup;

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


		//very basic at the moment, under dev (this was built for the kitchen knife which itemNum = 6)
		public void PickUpObject(Sprite itemSprite, int itemNum){

			if (PlayerScript.playerControl != null) {
				if (isRightHand && !PlayerScript.playerControl.playerSprites.isRightHandFull) {
					handSelector.rightRend.sprite = itemSprite;
					handSelector.rightRend.enabled = true;
					PlayerScript.playerControl.playerSprites.isRightHandSelector = true;
					PlayerScript.playerControl.playerSprites.isRightHandFull = true;
					PlayerScript.playerControl.playerSprites.PickedUpItem (itemNum);
			
				} else if (!isRightHand && !PlayerScript.playerControl.playerSprites.isLeftHandFull) {
					handSelector.leftRend.sprite = itemSprite;
					handSelector.leftRend.enabled = true;
					PlayerScript.playerControl.playerSprites.isRightHandSelector = false;
					PlayerScript.playerControl.playerSprites.isLeftHandFull = true;
					PlayerScript.playerControl.playerSprites.PickedUpItem (itemNum);
			
				}
			}

		}

			
}
}