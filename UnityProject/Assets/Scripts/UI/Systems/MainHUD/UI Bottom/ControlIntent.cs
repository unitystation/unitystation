using UnityEngine;
using UnityEngine.UI;

public enum Intent
{
	Help,
	Disarm,
	Grab,
	Harm
}

namespace UI
{
	public class ControlIntent : TooltipMonoBehaviour
	{
		public Sprite[] sprites;
		[SerializeField] private Image thisImg = default;

		public override string Tooltip => "intent";

		[Header("GameObject references")]
		[SerializeField] private GameObject runWalkBorder = default;
		[SerializeField] private GameObject helpWindow = default;
		[Header("Message settings")]
		[SerializeField] private string startRestMessage = "You try to lie down.";
		[SerializeField] private string endRestMessage = "You try to stand up.";
		[SerializeField] private string startRunningMessage = "You start running";
		[SerializeField] private string startWalkingMessage = "You start walking";

		private bool clientResting = false;

		public bool Running { get; set; } = true;

		private void Start()
		{
			SetIntent(Intent.Help);

			if (runWalkBorder == null)
			{
				// TODO: wait for UI changes to settle down before refactoring this to reflect the changes.
				Logger.LogWarning("At least one intent GameObject is unassigned.", Category.Interaction);
			}
			else
			{
				runWalkBorder.SetActive(Running);
			}
		}

		#region OnClick Listeners

		/// <summary>
		/// Called when player clicks Rest button
		/// </summary>
		public void OnClickRest()
		{
			Logger.Log("OnClickRest", Category.UserInput);
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			clientResting = !clientResting;
			RequestRest.Send(clientResting);
			Chat.AddExamineMsgToClient(clientResting ? startRestMessage : endRestMessage);
			// TODO: trigger rest intent
		}

		/// <summary>
		/// Called when player clicks Crafting button
		/// </summary>
		public void OnClickCrafting()
		{
			Logger.Log("OnClickCrafting", Category.UserInput);
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			UIManager.Instance.CraftingMenu.Open();
		}

		/// <summary>
		/// Called when player clicks Run/Walk button
		/// </summary>
		public void OnClickRunWalk()
		{
			Logger.Log("OnClickRunWalk", Category.UserInput);
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			Running = !Running;
			runWalkBorder.SetActive(Running);

			Chat.AddExamineMsgToClient(Running ? startRunningMessage : startWalkingMessage);
		}

		/// <summary>
		/// Called when player clicks Resist button
		/// </summary>
		public void OnClickResist()
		{
			Logger.Log("OnClickResist", Category.UserInput);
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			UIManager.Action.Resist();
		}

		/// <summary>
		/// Called when player clicks Help button
		/// </summary>
		public void OnClickHelp()
		{
			Logger.Log("OnClickHelp", Category.UserInput);
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			helpWindow.SetActive(!helpWindow.activeSelf);
		}

		#endregion

		public void CycleIntent(bool cycleLeft = true)
		{
			Logger.Log("Intent cycling " + (cycleLeft ? "left" : "right"), Category.UserInput);
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

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

			UpdateIcon(intent);
		}

		// OnClick method
		// The selected intent can be passed from a button in the UI
		public void IntentButton(int selectedIntent)
		{
			Logger.Log("Intent Button", Category.UserInput);

			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			UpdateIcon(selectedIntent);
		}

		// Hotkey method
		public void SetIntent(Intent intent)
		{
			UpdateIcon((int)intent);
		}

		private void UpdateIcon(int intent)
		{

			UIManager.CurrentIntent = (Intent)intent;
			if (thisImg != null && sprites[intent] != null)
			{
				thisImg.sprite = sprites[intent];
			}
		}
	}
}
