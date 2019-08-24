using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


	public class ControlWalkRun : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		private Image image;
		public Sprite[] runWalkSprites;

		public bool running { get; private set; } = true;

		private void Start()
		{
			image = GetComponent<Image>();
		}

		/* 
		* Button OnClick methods
		*/

		public void RunWalk()
		{
			Logger.Log("RunWalk Button", Category.UI);

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

		public void OnPointerEnter(PointerEventData eventData)
		{
			UIManager.SetToolTip = "run/walk toggle";
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			UIManager.SetToolTip = "";
		}
	}
