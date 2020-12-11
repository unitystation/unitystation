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
	[SerializeField] private Image thisImg = default;

	public override string Tooltip => "intent";

	[Header("GameObject references")]
	[SerializeField] private GameObject helpIntentIcon = default;
	[SerializeField] private GameObject harmIntentIcon = default;
	[SerializeField] private GameObject runWalkBorder = default;
	[SerializeField] private GameObject helpWindow = default;
	[Header("Message settings")]
	[SerializeField] private string restMessage = "You try to lie down.";
	[SerializeField] private string startRunningMessage = "You start running";
	[SerializeField] private string startWalkingMessage = "You start walking";

	public bool Running { get; set; } = true;

	private void Start()
	{
		SetIntent(Intent.Help);

		if (helpIntentIcon == null || harmIntentIcon == null || runWalkBorder == null)
		{
			// TODO: wait for UI changes to settle down before refactoring this to reflect the changes.
			Logger.LogWarning("At least one intent GameObject is unassigned.");
		}
		else
		{
			helpIntentIcon.SetActive(true);
			harmIntentIcon.SetActive(false);
			runWalkBorder.SetActive(Running);
		}
	}

	#region OnClick listeners

	/// <summary>
	/// Called when player clicks Intent button
	/// </summary>
	public void OnClickIntent()
	{
		Logger.Log("OnClickIntent", Category.UI);
		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		if (UIManager.CurrentIntent == Intent.Help)
		{
			helpIntentIcon.SetActive(false);
			harmIntentIcon.SetActive(true);

			UIManager.CurrentIntent = Intent.Harm;
		}
		else
		{
			helpIntentIcon.SetActive(true);
			harmIntentIcon.SetActive(false);

			UIManager.CurrentIntent = Intent.Help;
		}
	}

	/// <summary>
	/// Called when player clicks Rest button
	/// </summary>
	public void OnClickRest()
	{
		Logger.Log("OnClickRest", Category.UI);
		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		Chat.AddExamineMsgToClient(restMessage);

		// TODO: trigger rest intent
	}

	/// <summary>
	/// Called when player clicks Crafting button
	/// </summary>
	public void OnClickCrafting()
	{
		Logger.Log("OnClickCrafting", Category.UI);
		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		// TODO: crafting
	}

	/// <summary>
	/// Called when player clicks Run/Walk button
	/// </summary>
	public void OnClickRunWalk()
	{
		Logger.Log("OnClickRunWalk", Category.UI);
		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		Running = !Running;
		runWalkBorder.SetActive(Running);

		Chat.AddExamineMsgToClient(Running ? startRunningMessage : startWalkingMessage);
	}

	/// <summary>
	/// Called when player clicks Resist button
	/// </summary>
	public void OnClickResist()
	{
		Logger.Log("OnClickResist", Category.UI);
		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		UIManager.Action.Resist();
	}

	/// <summary>
	/// Called when player clicks Help button
	/// </summary>
	public void OnClickHelp()
	{
		Logger.Log("OnClickHelp", Category.UI);
		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		helpWindow.SetActive(!helpWindow.activeSelf);
	}

	#endregion

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
		//if (thisImg != null) thisImg.sprite = sprites[intent];
	}


	//OnClick method
	//The selected intent can be passed from a button in the UI
	public void IntentButton(int selectedIntent)
	{
		Logger.Log("Intent Button", Category.UI);

		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		UIManager.CurrentIntent = (Intent) selectedIntent;

		thisImg.sprite = sprites[selectedIntent];
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
