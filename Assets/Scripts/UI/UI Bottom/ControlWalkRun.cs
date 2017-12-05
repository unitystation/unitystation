using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI
{
    public class ControlWalkRun : MonoBehaviour
    {

        public Sprite[] runWalkSprites;

        public bool running { get; set; }

        private Image image;

        void Start()
        {

            image = GetComponent<Image>();

        }

        /* 
		 * Button OnClick methods
		 */

        public void RunWalk()
        {
            Debug.Log("RunWalk Button");

            SoundManager.Play("Click01");

            if (!running)
            {
                running = true;
                image.sprite = runWalkSprites[1];

            }
            else
            {
                running = false;
                image.sprite = runWalkSprites[0];
            }
        }
    }
}