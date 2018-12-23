using UnityEngine;
using UnityEngine.UI;

	public enum Intent
	{
		Help,
		Disarm,
		Grab,
		Harm
	}

	public class ControlIntent : MonoBehaviour
	{
		public Sprite[] sprites;
		private Image thisImg;

		private void Start()
		{
			thisImg = GetComponent<Image>();
			SetIntent(Intent.Help);
		}

		//OnClick method
		public void IntentButton()
		{
			Logger.Log("Intent Button", Category.UI);

			SoundManager.Play("Click01");

			int intent = (int) UIManager.CurrentIntent;
			intent = (intent + 1) % 4;

			UIManager.CurrentIntent = (Intent) intent;

			thisImg.sprite = sprites[intent];
		}

		public void CycleIntent(bool cycleLeft = true)
		{
			Logger.Log("Intent cycling " + (cycleLeft ? "left" : "right"), Category.UI);
			SoundManager.Play("Click01");

			int intent = (int) UIManager.CurrentIntent;
			intent += (cycleLeft ? 1 : -1);

			// Assuming we never add more than 4 intents
			if (intent == -1)
			{
				intent = 3;
			}
			else if (intent == 4)
			{
				intent = 0;
			}

			UIManager.CurrentIntent = (Intent) intent;
			thisImg.sprite = sprites[intent];
		}

        //Hotkey method
        public void SetIntent(Intent intent)
        {
            UIManager.CurrentIntent = intent;

            thisImg.sprite = sprites[(int)intent];
        }
	}
