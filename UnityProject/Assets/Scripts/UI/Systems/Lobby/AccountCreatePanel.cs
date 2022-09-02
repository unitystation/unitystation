﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Core.Utils;
using DatabaseAPI;

namespace Lobby
{
	/// <summary>
	/// Scripting for the account creation panel found in the lobby UI.
	/// </summary>
	public class AccountCreatePanel : MonoBehaviour
	{
		[SerializeField]
		private InputField emailControl = default;
		[SerializeField]
		private InputField usernameControl = default;
		[SerializeField]
		private InputField passwordControl = default;

		[SerializeField]
		private Text errorControl = default;

		[SerializeField]
		private Button backButton = default;
		[SerializeField]
		private Button submitButton = default;

		private void Awake()
		{
			emailControl.onValueChanged.AddListener((_) => ClearError());
			emailControl.onEndEdit.AddListener((_) => ValidateEmail());

			passwordControl.onValueChanged.AddListener((_) => ClearError());
			passwordControl.onEndEdit.AddListener((_) => ValidatePassword());

			usernameControl.onValueChanged.AddListener((_) => ClearError());
			usernameControl.onEndEdit.AddListener((_) => ValidateUsername());

			backButton.onClick.AddListener(OnBackBtn);
			submitButton.onClick.AddListener(OnSubmitBtn);
		}

		private void OnEnable()
		{
			ClearError();
			emailControl.Select();
			emailControl.ActivateInputField();
		}

		private void Reset()
		{
			emailControl.text = string.Empty;
			usernameControl.text = string.Empty;
			passwordControl.text = string.Empty;

			ClearError();
		}

		#region Validation

		private bool ValidateInputs()
		{
			if (ValidateEmail() == false) return false;

			if (ValidateUsername() == false) return false;

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

		private bool ValidateUsername()
		{
			var errorStrings = new Dictionary<ValidationUtils.ValidationError, string>
			{
				{ ValidationUtils.ValidationError.NullOrWhitespace, "Username is required." },
				{ ValidationUtils.ValidationError.TooShort, "Username is too short." },
				{ ValidationUtils.ValidationError.Invalid, "Username is invalid." },
			};

			if (ValidationUtils.TryValidateUsername(usernameControl.text, out var failReason) == false)
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

		#endregion

		private void CreateAccount()
		{
			if (ValidateInputs() == false) return;

			LobbyManager.UI.ShowLoadingPanel(new LoadingPanelArgs {
				Text = "Creating your account...",
				RightButtonLabel = "Cancel",
				// TODO: implement cancellation
				RightButtonCallback = () => throw new NotImplementedException(),
			});

			ServerData.TryCreateAccount(usernameControl.text, passwordControl.text, emailControl.text,
					ShowInfoPanelSuccess,
					ShowInfoPanelFail);
		}

		private void ResendEmail(string email)
		{
			FirebaseAuth.DefaultInstance.CurrentUser.SendEmailVerificationAsync();
			FirebaseAuth.DefaultInstance.SignOut();

			LobbyManager.UI.ShowEmailResendPanel(email);
		}

		private void ShowInfoPanelSuccess(FirebaseUser account)
		{
			LobbyManager.UI.ShowInfoPanel(new InfoPanelArgs
			{
				Heading = "Account Created",
				Text = $"Success! An email will be sent to\n<b>{account.Email}</b>\n\n" +
					$"Please click the link in the email to verify your account before signing in.",
				LeftButtonLabel = "Back",
				LeftButtonCallback = LobbyManager.UI.ShowLoginPanel,
				RightButtonLabel = "Resend Email",
				RightButtonCallback = () => ResendEmail(account.Email),
			});

			PlayerPrefs.SetString(PlayerPrefKeys.AccountEmail, emailControl.text);
			PlayerPrefs.Save();

			Reset();
		}

		private void ShowInfoPanelFail(string errorText)
		{
			LobbyManager.UI.ShowInfoPanel(new InfoPanelArgs
			{
				Heading = "Account Creation Failed",
				Text = errorText,
				IsError = true,
				LeftButtonLabel = "Back",
				LeftButtonCallback = LobbyManager.UI.ShowAccountCreatePanel,
				RightButtonLabel = "Retry",
				RightButtonCallback = CreateAccount,
			});
		}

		#region Button Handlers

		private void OnBackBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.UI.ShowLoginPanel();
		}

		private void OnSubmitBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (ValidateInputs())
			{
				CreateAccount();
			}
		}

		#endregion
	}
}
