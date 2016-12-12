using UnityEngine;
using UnityEngine.UI;


namespace UI {
    public class OxygenButton: MonoBehaviour {

        public Sprite[] stateSprites;
        private Image image;
        // Use this for initialization
        void Start() {
            image = GetComponent<Image>();
            UIManager.control.isOxygen = false;
        }

        public void OxygenSelect() {

            SoundManager.control.Play("Click01");
            if(!UIManager.control.isOxygen) {

                UIManager.control.isOxygen = true;
                image.sprite = stateSprites[1];

            } else {
                UIManager.control.isOxygen = false;
                image.sprite = stateSprites[0];
            }
        }
    }
}