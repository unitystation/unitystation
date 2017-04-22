using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class CritListener : MonoBehaviour
    {
        public Sprite critSprite;

        public void ShowCrit()
        {
            Debug.Log("Setting Crit");
            SoundManager.Play("Critstate");
            GetComponent<Image>().sprite = critSprite;
        }
    }
}