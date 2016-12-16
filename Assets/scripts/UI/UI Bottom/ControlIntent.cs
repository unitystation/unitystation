using UnityEngine;
using UnityEngine.UI;

namespace UI {

    public enum Intent {
        Help,
        Disarm,
        Hold,
        Attack
    }

    public class ControlIntent: MonoBehaviour {
        public Sprite[] sprites;
        private Image thisImg;

        void Start() {

            UIManager.control.currentIntent = Intent.Help;
            thisImg = GetComponent<Image>();
        }

        //OnClick method
        public void IntentButton() {
            Debug.Log("Intent Button");

            SoundManager.control.Play("Click01");

            int intent = (int) UIManager.control.currentIntent;
            intent = (intent + 1) % 4;

            UIManager.control.currentIntent = (Intent) intent;

            thisImg.sprite = sprites[intent];
        }
    }
}