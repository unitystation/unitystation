using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PlayGroup;


namespace UI {
    public class ControlAction: MonoBehaviour {

        public Sprite[] throwSprites;
        public Image throwImage;

        void Start() {
            UIManager.IsThrow = false;
        }

        /* 
		 * Button OnClick methods
		 */

        void Update() {
            CheckKeyboardInput();
        }

        void CheckKeyboardInput() {
            if(Input.GetKeyDown(KeyCode.Q)) {
                Drop();
            }

			if(Input.GetKeyDown(KeyCode.X)) {
				UIManager.Hands.Swap();
			}

			if (Input.GetKeyDown(KeyCode.E)) {
				UIManager.Hands.Use();
			}
        }

        public void Resist() {
            SoundManager.Play("Click01");
            Debug.Log("Resist Button");
        }

        public void Drop() {
            //ClientApproval
            var lps = PlayerManager.LocalPlayerScript;
            if ( !lps || lps.canNotInteract() || !UIManager.Hands.CurrentSlot.Item ) return;
            //Message
            lps.playerNetworkActions.DropItem(UIManager.Hands.CurrentSlot.eventName);
            
            //ClientPrediction
            SoundManager.Play("Click01");
            Debug.Log("Drop Button");
            //TODO! common client drop method
            GameObject item = UIManager.Hands.CurrentSlot.PlaceItemInScene();
            item.transform.parent = null;
            item.transform.position = gameObject.transform.position;
            var e = item.GetComponent<EditModeControl>();
            e.Snap();
			item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
        }

        public void Throw() {
            var lps = PlayerManager.LocalPlayerScript;
            if ( !lps || lps.canNotInteract()) return;

            SoundManager.Play("Click01");
            Debug.Log("Throw Button");

            if(!UIManager.IsThrow) {
                UIManager.IsThrow = true;
                throwImage.sprite = throwSprites[1];

            } else {
                UIManager.IsThrow = false;
                throwImage.sprite = throwSprites[0];
            }
        }
    }
}