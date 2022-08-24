using System;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	/// <summary>
	/// Scripting for the reusable loading panel found in the lobby UI.
	/// </summary>
	public class LoadingPanel : MonoBehaviour
	{
		[SerializeField]
		private Text textControl = default;

		[SerializeField]
		private Button leftButton = default;
		[SerializeField]
		private Button rightButton = default;

		private Text leftButtonLabel;
		private Text rightButtonLabel;

		private void Awake()
		{
			leftButtonLabel = leftButton.GetComponentInChildren<Text>();
			rightButtonLabel = rightButton.GetComponentInChildren<Text>();
		}

		public void Reset()
		{
			textControl.text = "Loading...";

			leftButton.gameObject.SetActive(false);
			rightButton.gameObject.SetActive(false);

			leftButton.onClick.RemoveAllListeners();
			rightButton.onClick.RemoveAllListeners();

			leftButtonLabel.text = string.Empty;
			rightButtonLabel.text = string.Empty;
		}

		public void Show(LoadingPanelArgs args)
		{
			Reset();

			SetText(args.Text);

			if (args.LeftButtonCallback != null)
			{
				SetupLeftButton(args.LeftButtonCallback, args.LeftButtonText);
			}

			if (args.RightButtonCallback != null)
			{
				SetupRightButton(args.RightButtonCallback, args.RightButtonText);
			}
		}

		public void SetText(string text)
		{
			textControl.text = text;
		}

		public void SetupLeftButton(Action callback, string buttonLabel)
		{
			leftButtonLabel.text = buttonLabel;
			leftButton.onClick.AddListener(() => {
				_ = SoundManager.Play(CommonSounds.Instance.Click01);
				callback.Invoke();
			});
			leftButton.gameObject.SetActive(true);
		}

		public void SetupRightButton(Action callback, string buttonLabel)
		{
			rightButtonLabel.text = buttonLabel;
			rightButton.onClick.AddListener(() =>
			{
				_ = SoundManager.Play(CommonSounds.Instance.Click01);
				callback.Invoke();
			});
			rightButton.gameObject.SetActive(true);
		}
	}

	public class LoadingPanelArgs
	{
		public string Text { get; set; }
		public Action LeftButtonCallback { get; set; }
		public Action RightButtonCallback { get; set; }
		public string LeftButtonText { get; set; }
		public string RightButtonText { get; set; }
	}
}
