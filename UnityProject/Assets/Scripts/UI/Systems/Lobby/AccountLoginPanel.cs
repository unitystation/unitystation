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
		private InputField emailControl = default;
		[SerializeField]
		private InputField passwordControl = default;
		[SerializeField]
		private Text errorControl = default;
		[SerializeField]
		private Toggle autoLoginControl = default;
		[SerializeField]
		private Button loginButtonControl = default;

		public bool IsAutoLoginEnabled => autoLoginControl.isOn;

		private void Awake()
		{
			emailControl.onValueChanged.AddListener((_) => ClearError());
			emailControl.onEndEdit.AddListener((_) => ValidateEmail());
			emailControl.onSubmit.AddListener((_) => TryLogin());

			passwordControl.onValueChanged.AddListener((_) => ClearError());
			passwordControl.onEndEdit.AddListener((_) => ValidatePassword());
			passwordControl.onSubmit.AddListener((_) => TryLogin());

			loginButtonControl.onClick.AddListener(() => OnLoginBtn());
		}

		private void Start()
		{
			emailControl.text = PlayerPrefs.GetString(PlayerPrefKeys.AccountEmail);
		}

		public void SetEmailField(string newEmail) => emailControl.text = newEmail;

		private void TryLogin()
		{
			if (ValidateLogin() == false) return;

			_ = LobbyManager.Instance.TryLogin(emailControl.text, passwordControl.text);
			passwordControl.text = string.Empty;
		}

		private bool ValidateLogin()
		{
			if (ValidateEmail() == false) return false;

			if (ValidatePassword() == false) return false;

			return true;
		}

		private bool ValidateEmail()
		{
			var errorStrings = new Dictionary<ValidationUtils.StringValidateError, string>
			{
				{ ValidationUtils.StringValidateError.NullOrWhitespace, "Email address is required." },
				{ ValidationUtils.StringValidateError.Invalid, "Email address is invalid." },
			};

			if (ValidationUtils.TryValidateEmail(emailControl.text, out var failReason) == false)
			{
				SetError(errorStrings[failReason]);
				return false;
			}

			ClearError();
			return true;
		}

		private bool ValidatePassword()
		{
			var errorStrings = new Dictionary<ValidationUtils.StringValidateError, string>
			{
				{ ValidationUtils.StringValidateError.NullOrWhitespace, "Password is required." },
				{ ValidationUtils.StringValidateError.TooShort, "Password is too short." },
				{ ValidationUtils.StringValidateError.Invalid, "Password is invalid." },
			};

			if (ValidationUtils.TryValidatePassword(passwordControl.text, out var failReason) == false)
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
