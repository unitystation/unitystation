using UnityEngine;
using UnityEngine.UI;
using Core.Utils;
using System.Collections.Generic;

namespace Lobby
{
	/// <summary>
	/// Scripting for the account login panel found in the lobby UI.
	/// </summary>
	public class AccountLoginPanel : MonoBehaviour
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
			if (ValidateEmail() == false) return false;

			if (ValidatePassword() == false) return false;

			return true;
		}

		private bool ValidateEmail()
		{
			var errorStrings = new Dictionary<ValidationUtils.StringValidateError, string>()
			{
				{ ValidationUtils.StringValidateError.NullOrWhitespace, "Email address is required." },
				{ ValidationUtils.StringValidateError.Invalid, "Email address is invalid." },
			};

			if (ValidationUtils.ValidateEmail(emailControl.text, out var failReason) == false)
			{
				SetError(errorStrings[failReason]);
				return false;
			}

			ClearError();
			return true;
		}

		private bool ValidatePassword()
		{
			var errorStrings = new Dictionary<ValidationUtils.StringValidateError, string>()
			{
				{ ValidationUtils.StringValidateError.NullOrWhitespace, "Password is required." },
				{ ValidationUtils.StringValidateError.TooShort, "Password is too short." },
				{ ValidationUtils.StringValidateError.Invalid, "Password is invalid." },
			};

			if (ValidationUtils.ValidatePassword(passwordControl.text, out var failReason) == false)
			{
				SetError(errorStrings[failReason]);
				return false;
			}

			ClearError();
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
