using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class DeadListener : MonoBehaviour
    {
        public Sprite deadSprite;

        public void ShowDead()
        {
            Debug.Log("Setting Dead");
            GetComponent<Image>().sprite = deadSprite;
        }
    }
}