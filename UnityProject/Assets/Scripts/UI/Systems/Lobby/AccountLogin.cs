using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	/// <summary>
	/// Scripting for the account login screen.
	/// </summary>
	public class AccountLogin : MonoBehaviour
	{
		[SerializeField]
		private InputField emailControl;
		[SerializeField]
		private InputField passwordControl;
		[SerializeField]
		private Text errorControl;
		[SerializeField]
		private Toggle autoLoginControl;
		[SerializeField]
		private Button loginButtonControl;

		public bool IsAutoLoginEnabled => autoLoginControl.isOn;

		private void Awake()
		{
			emailControl.onValueChanged.AddListener((value) => ClearError());
			emailControl.onEndEdit.AddListener((value) => ValidateEmail());
			emailControl.onSubmit.AddListener((value) => TryLogin());

			passwordControl.onValueChanged.AddListener((value) => ClearError());
			passwordControl.onEndEdit.AddListener((value) => ValidatePassword());
			passwordControl.onSubmit.AddListener((value) => TryLogin());

			loginButtonControl.onClick.AddListener(() => OnLoginBtn());
		}

		private void Start()
		{
			emailControl.text = PlayerPrefs.GetString(PlayerPrefKeys.AccountEmail);
		}

		public void SetEmailField(string newEmail) => emailControl.text = newEmail;

		private void TryLogin()
		{
			if (!ValidateLogin()) return;

			var password = passwordControl.text;
			passwordControl.text = string.Empty;

			LobbyManager.Instance.TryLogin(emailControl.text, password);
		}

		private bool ValidateLogin()
		{
			if (!ValidateEmail()) return false;

			if (!ValidatePassword()) return false;

			return true;
		}

		private bool ValidateEmail()
		{
			if (string.IsNullOrWhiteSpace(emailControl.text))
			{
				errorControl.text = "Email address is required.";
				return false;
			}

			// 👀 courtesy of https://uibakery.io/regex-library/email-regex-csharp
			string pattern = "(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])";
			if (new Regex(pattern).IsMatch(emailControl.text) == false)
			{
				errorControl.text = "Email address is invalid.";
				return false;
			}

			errorControl.text = string.Empty;
			return true;
		}

		private bool ValidatePassword()
		{
			if (string.IsNullOrWhiteSpace(passwordControl.text))
			{
				SetError("Password is required.");
				return false;
			}

			return true;
		}

		private void SetError(string message) => errorControl.text = message;

		private void ClearError() => errorControl.text = string.Empty;

		private void OnLoginBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			TryLogin();
		}
	}
}
