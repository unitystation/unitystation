using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Core.Utils;

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
		private Button createButtonControl = default;
		[SerializeField]
		private Button loginButtonControl = default;
		[SerializeField]
		private Button exitButtonControl = default;

		[SerializeField]
		private Button OffLineButtonControl = default;

		private void Awake()
		{
			emailControl.onValueChanged.AddListener((_) => ClearError());
			emailControl.onEndEdit.AddListener((_) => ValidateEmail());
			emailControl.onSubmit.AddListener((_) => TryLogin());

			passwordControl.onValueChanged.AddListener((_) => ClearError());
			passwordControl.onEndEdit.AddListener((_) => ValidatePassword());
			passwordControl.onSubmit.AddListener((_) => TryLogin());

			createButtonControl.onClick.AddListener(OnCreateBtn);
			loginButtonControl.onClick.AddListener(OnLoginBtn);
			exitButtonControl.onClick.AddListener(OnExitBtn);

			OffLineButtonControl.onClick.AddListener(OnOffLineMode);
		}

		private void Start()
		{
			emailControl.text = PlayerPrefs.GetString(PlayerPrefKeys.AccountEmail);
		}

		private void OnEnable()
		{
			Reset();

			if (string.IsNullOrEmpty(emailControl.text) == false)
			{
				passwordControl.Select();
				passwordControl.ActivateInputField();
				return;
			}

			emailControl.text = PlayerPrefs.GetString(PlayerPrefKeys.AccountEmail);

			if (string.IsNullOrEmpty(emailControl.text))
			{
				emailControl.Select();
				emailControl.ActivateInputField();
			}
		}

		private void OnDisable()
		{
			passwordControl.text = string.Empty;
		}

		private void Reset()
		{
			passwordControl.text = string.Empty;
			ClearError();
		}

		private void TryLogin()
		{
			if (ValidateLogin() == false) return;

			LobbyManager.Instance.SetAutoLogin(autoLoginControl.isOn);

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
			var errorStrings = new Dictionary<ValidationUtils.ValidationError, string>
			{
				{ ValidationUtils.ValidationError.NullOrWhitespace, "Email address is required." },
				{ ValidationUtils.ValidationError.Invalid, "Email address is invalid." },
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
			var errorStrings = new Dictionary<ValidationUtils.ValidationError, string>
			{
				{ ValidationUtils.ValidationError.NullOrWhitespace, "Password is required." },
				{ ValidationUtils.ValidationError.TooShort, "Password is too short." },
				{ ValidationUtils.ValidationError.Invalid, "Password is invalid." },
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

		private void OnOffLineMode()
		{
			GameData.Instance.SetForceOfflineMode(true);
			LobbyManager.UI.ShowMainPanel();
		}

		private void OnLoginBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			TryLogin();
		}

		private void OnCreateBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.UI.ShowAccountCreatePanel();
		}

		private void OnExitBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.Instance.Quit();
		}
	}
}
