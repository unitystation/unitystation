using UnityEngine;
using UnityEngine.UI;
using System.Collections;


namespace UI {
    public class ControlAction: MonoBehaviour {

        public Sprite[] throwSprites;
        public Image throwImage;

        void Start() {
            UIManager.control.isThrow = false;
        }

        /* 
		 * Button OnClick methods
		 */

        public void Resist() {
            PlayClick01();
            Debug.Log("Resist Button");
        }

        public void Drop() {
            PlayClick01();
            Debug.Log("Drop Button");
        }

        public void Throw() {
            PlayClick01();
            Debug.Log("Throw Button");

            if(!UIManager.control.isThrow) {
                UIManager.control.isThrow = true;
                throwImage.sprite = throwSprites[1];

            } else {
                UIManager.control.isThrow = false;
                throwImage.sprite = throwSprites[0];
            }
        }

        //SoundFX

        void PlayClick01() {
            if(SoundManager.control != null) {
                SoundManager.control.Play("Click01");
            }
        }
    }
}