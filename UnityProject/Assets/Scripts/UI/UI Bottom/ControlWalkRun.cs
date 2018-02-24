using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ControlWalkRun : MonoBehaviour
	{
		private Image image;
		public Sprite[] runWalkSprites;

		public bool running { get; set; }

		private void Start()
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