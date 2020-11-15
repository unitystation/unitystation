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
	public override string Tooltip => "intent";

	[Header("GameObject references")]
	[SerializeField] private GameObject helpIntentIcon;
	[SerializeField] private GameObject harmIntentIcon;
	[SerializeField] private GameObject runWalkBorder;
	[SerializeField] private GameObject helpWindow;
	[Header("Message settings")]
	[SerializeField] private string restMessage = "You try to lie down.";
	[SerializeField] private string startRunningMessage = "You start running";
	[SerializeField] private string startWalkingMessage = "You start walking";

	public bool running { get; set; } = true;

	private void Start()
	{
		SetIntent(Intent.Help);
		helpIntentIcon.SetActive(true);
		harmIntentIcon.SetActive(false);

		runWalkBorder.SetActive(running);
	}

	#region OnClick listeners

	/// <summary>
	/// Called when player clicks Intent button
	/// </summary>
	public void OnClickIntent()
	{
		Logger.Log("OnClickIntent", Category.UI);
		SoundManager.Play("Click01");

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
		SoundManager.Play("Click01");

		Chat.AddExamineMsgToClient(restMessage);

		// TODO: trigger rest intent
	}

	/// <summary>
	/// Called when player clicks Crafting button
	/// </summary>
	public void OnClickCrafting()
	{
		Logger.Log("OnClickCrafting", Category.UI);
		SoundManager.Play("Click01");

		// TODO: crafting
	}

	/// <summary>
	/// Called when player clicks Run/Walk button
	/// </summary>
	public void OnClickRunWalk()
	{
		Logger.Log("OnClickRunWalk", Category.UI);
		SoundManager.Play("Click01");
		
		running = !running;
		runWalkBorder.SetActive(running);
		
		Chat.AddExamineMsgToClient(running ? startRunningMessage : startWalkingMessage);
	}

	/// <summary>
	/// Called when player clicks Resist button
	/// </summary>
	public void OnClickResist()
	{
		Logger.Log("OnClickResist", Category.UI);
		SoundManager.Play("Click01");

		UIManager.Action.Resist();
	}

	/// <summary>
	/// Called when player clicks Help button
	/// </summary>
	public void OnClickHelp()
	{
		Logger.Log("OnClickHelp", Category.UI);
		SoundManager.Play("Click01");

		helpWindow.SetActive(!helpWindow.activeSelf);
	}

	#endregion

	public void CycleIntent(bool cycleLeft = true)
	{
		Logger.Log("Intent cycling " + (cycleLeft ? "left" : "right"), Category.UI);
		SoundManager.Play("Click01");

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

	//Hotkey method
	public void SetIntent(Intent intent)
	{
		UIManager.CurrentIntent = intent;

		// if (thisImg != null)
		// {
		// 	thisImg.sprite = sprites[(int)intent];
		// }
	}
}