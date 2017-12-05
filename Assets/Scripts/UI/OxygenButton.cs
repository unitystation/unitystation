using UnityEngine;
using UnityEngine.UI;


namespace UI
{
    public class OxygenButton : MonoBehaviour
    {

        public Sprite[] stateSprites;
        private Image image;
        // Use this for initialization
        void Start()
        {
            image = GetComponent<Image>();
            UIManager.IsOxygen = false;
        }

        public void OxygenSelect()
        {

            SoundManager.Play("Click01");
            if (!UIManager.IsOxygen)
            {

                UIManager.IsOxygen = true;
                image.sprite = stateSprites[1];

            }
            else
            {
                UIManager.IsOxygen = false;
                image.sprite = stateSprites[0];
            }
        }
    }
}