using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.PreRound
{
	public class PreRoundButtonsScreen : MonoBehaviour
	{
		[SerializeField] private Transform adminPanel = null;
		[SerializeField] private Transform buttonsArea = null;

		[SerializeField] private TMP_Text title = null;
		[SerializeField] private TMP_Text gameModeDisplay = null;
		[SerializeField] public TMP_Text playerCountNumber = null;

		[SerializeField] private Transform buttonTemplate = null;
		[SerializeField] private Transform toggleTemplate = null;

		public Button CreateInteractableButton(string buttonText, System.Action onClick)
		{
			var newButton = Instantiate(buttonTemplate, buttonsArea);
			if (newButton == null || newButton.TryGetComponent<Button>(out var btn) == false)
			{
				Debug.LogError("Button template is null");
				return null;
			}
			newButton.gameObject.SetActive(true);
			newButton.GetComponentInChildren<TMP_Text>().text = buttonText;
			btn.onClick.AddListener(() =>
			{
				_ = SoundManager.Play(CommonSounds.Instance.Click01);
				onClick?.Invoke();
			});
			return btn;
		}

		public Toggle CreateInteractableToggle(string buttonText, System.Action<bool> onClick)
		{
			var newButton = Instantiate(toggleTemplate, buttonsArea);
			if (newButton == null || newButton.TryGetComponent<Toggle>(out var btn) == false)
			{
				Debug.LogError("Button template is null");
				return null;
			}
			newButton.gameObject.SetActive(true);
			newButton.GetComponentInChildren<TMP_Text>().text = buttonText;
			btn.onValueChanged.AddListener((value) =>
			{
				_ = SoundManager.Play(CommonSounds.Instance.Click01);
				onClick?.Invoke(value);
			});
			return btn;
		}

		public void SetPlayerCount(int playerCount)
		{
			playerCountNumber.text = playerCount.ToString();
		}

		public void SetTitle(string newTitle)
		{
			title.text = newTitle;
		}

		public void RefreshGameModeText()
		{
			gameModeDisplay.text = "Game Mode: " + GameManager.Instance.GetGameModeName();
		}
	}
}