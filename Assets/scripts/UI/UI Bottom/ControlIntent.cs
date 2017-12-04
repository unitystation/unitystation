using UnityEngine;
using UnityEngine.UI;

namespace UI
{

    public enum Intent
    {
        Help,
        Disarm,
        Hold,
        Attack
    }

    public class ControlIntent : MonoBehaviour
    {
        public Sprite[] sprites;
        private Image thisImg;

        void Start()
        {

            UIManager.CurrentIntent = Intent.Help;
            thisImg = GetComponent<Image>();
        }

        //OnClick method
        public void IntentButton()
        {
            Debug.Log("Intent Button");

            SoundManager.Play("Click01");

            int intent = (int)UIManager.CurrentIntent;
            intent = (intent + 1) % 4;

            UIManager.CurrentIntent = (Intent)intent;

            thisImg.sprite = sprites[intent];
        }
    }
}