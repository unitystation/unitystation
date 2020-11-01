using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum Intent
{
	Help,
	Disarm,
	Grab,
	Harm
}

public class ControlIntent : TooltipMonoBehaviour
{
	public Sprite[] sprites;
	private Image thisImg;
	public override string Tooltip => "intent";

	private void Start()
	{
		thisImg = GetComponent<Image>();
		SetIntent(Intent.Help);
	}

	//OnClick method
	//The selected intent can be passed from a button in the UI
	public void IntentButton(int selectedIntent)
	{
		Logger.Log("Intent Button", Category.UI);


		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		UIManager.CurrentIntent = (Intent)selectedIntent;

		thisImg.sprite = sprites[selectedIntent];
	}

	public void CycleIntent(bool cycleLeft = true)
	{
		Logger.Log("Intent cycling " + (cycleLeft ? "left" : "right"), Category.UI);

		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		int intent = (int)UIManager.CurrentIntent;
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

		UIManager.CurrentIntent = (Intent)intent;
		if (thisImg != null) thisImg.sprite = sprites[intent];
	}

	//Hotkey method
	public void SetIntent(Intent intent)
	{
		UIManager.CurrentIntent = intent;

		if (thisImg != null)
		{
			thisImg.sprite = sprites[(int)intent];
		}
	}
}
