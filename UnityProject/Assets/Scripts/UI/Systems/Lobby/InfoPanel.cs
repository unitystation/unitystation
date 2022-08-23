using System;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	/// <summary>
	/// Scripting for the reusable information panel found in the lobby UI.
	/// </summary>
	public class InfoPanel : MonoBehaviour
	{
		[SerializeField]
		private Text headingControl;
		[SerializeField]
		private Text textControl;

		[SerializeField]
		private Button leftButton;
		[SerializeField]
		private Button rightButton;

		private Text leftButtonLabel;
		private Text rightButtonLabel;

		private void Awake()
		{
			leftButtonLabel = leftButton.GetComponentInChildren<Text>();
			rightButtonLabel = rightButton.GetComponentInChildren<Text>();
		}

		public void Reset()
		{
			headingControl.text = "Information";
			textControl.text = string.Empty;
			textControl.color = Color.white;

			leftButton.gameObject.SetActive(false);
			rightButton.gameObject.SetActive(false);

			leftButton.onClick.RemoveAllListeners();
			rightButton.onClick.RemoveAllListeners();

			leftButtonLabel.text = string.Empty;
			rightButtonLabel.text = string.Empty;
		}

		public void Show(InfoPanelArgs args)
		{
			Reset();

			SetHeading(args.Heading);
			SetText(args.Text, args.IsError);

			if (args.LeftButtonCallback != null)
			{
				SetupLeftButton(args.LeftButtonCallback, args.LeftButtonText);
			}

			if (args.RightButtonCallback != null)
			{
				SetupRightButton(args.RightButtonCallback, args.RightButtonText);
			}
		}

		public void SetHeading(string headingText)
		{
			headingControl.text = headingText;
		}

		public void SetText(string text, bool isError = false)
		{
			textControl.text = text;
			if (isError)
			{
				textControl.color = Color.red;
			}
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

	public class InfoPanelArgs
	{
		public string Heading;
		public string Text;
		public bool IsError;
		public Action LeftButtonCallback;
		public Action RightButtonCallback;
		public string LeftButtonText;
		public string RightButtonText;
	}
}
