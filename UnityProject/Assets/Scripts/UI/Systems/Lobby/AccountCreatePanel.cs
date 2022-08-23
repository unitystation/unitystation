using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Core.Utils;
using DatabaseAPI;
using Firebase.Auth;

namespace Lobby
{
	public class AccountCreatePanel : MonoBehaviour
	{
		[SerializeField]
		private InputField emailControl;
		[SerializeField]
		private InputField usernameControl;
		[SerializeField]
		private InputField passwordControl;

		[SerializeField]
		private Text errorControl; // TODO create this in prefab

		[SerializeField]
		private Button backButton;
		[SerializeField]
		private Button createButton;

		private GUI_LobbyDialogue lobbyDialogue;

		private void Awake()
		{
			backButton.onClick.AddListener(OnBackBtn);
			createButton.onClick.AddListener(OnCreateBtn);
		}

		private void Reset()
		{
			emailControl.text = string.Empty;
			usernameControl.text = string.Empty;
			passwordControl.text = string.Empty;
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

		private bool ValidateUsername()
		{
			var errorStrings = new Dictionary<ValidationUtils.StringValidateError, string>()
			{
				{ ValidationUtils.StringValidateError.NullOrWhitespace, "Username is required." },
				{ ValidationUtils.StringValidateError.TooShort, "Username is too short." },
				{ ValidationUtils.StringValidateError.Invalid, "Username is invalid." },
			};

			if (ValidationUtils.ValidatePassword(passwordControl.text, out var failReason) == false)
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

		#endregion

		private void CreateAccount()
		{
			if (ValidateInputs() == false) return;

			lobbyDialogue.ShowLoadingPanel(new LoadingPanelArgs {
				Text = "Creating your account...",
				RightButtonText = "Cancel",
				// TODO: implement cancellation
				RightButtonCallback = () => throw new NotImplementedException(),
			});

			ServerData.TryCreateAccount(usernameControl.text, passwordControl.text, emailControl.text,
					ShowInfoPanelSuccess, ShowInfoPanelFail);
		}

		private void ResendEmail()
		{
			lobbyDialogue.ShowInfoPanel(new InfoPanelArgs
			{
				Heading = "Email Resend",
				Text = $"A new verification email will be sent to {FirebaseAuth.DefaultInstance.CurrentUser.Email}.",
				LeftButtonText = "Back",
				LeftButtonCallback = lobbyDialogue.ShowLoginScreen,
			});

			FirebaseAuth.DefaultInstance.CurrentUser.SendEmailVerificationAsync();
			FirebaseAuth.DefaultInstance.SignOut();
		}

		// TODO: TryCreateAccount expects CharacterSheet param for Success... should be account stuff
		private void ShowInfoPanelSuccess(CharacterSheet charSettings)
		{
			lobbyDialogue.ShowInfoPanel(new InfoPanelArgs
			{
				Heading = "Account Created",
				Text = $"Success! An email has been sent to {emailControl.text}.<br>" +
					$"Please click the link in the email to verify your account before signing in.",
				LeftButtonText = "Back",
				LeftButtonCallback = lobbyDialogue.ShowLoginScreen,
				RightButtonText = "Resend Email",
				RightButtonCallback = ResendEmail,
			});

			GameData.LoggedInUsername = usernameControl.text; // TODO why
			lobbyDialogue.LoginUIScript.SetEmailField(usernameControl.text);

			
			PlayerPrefs.SetString(PlayerPrefKeys.AccountEmail, emailControl.text);
			PlayerPrefs.Save();

			Reset();
		}

		private void ShowInfoPanelFail(string errorText)
		{
			lobbyDialogue.ShowInfoPanel(new InfoPanelArgs
			{
				Heading = "Account Creation Failed",
				Text = errorText,
				IsError = true,
				LeftButtonText = "Back",
				LeftButtonCallback = () => {
					_ = SoundManager.Play(CommonSounds.Instance.Click01);
					lobbyDialogue.ShowCreationPanel();
				},
				RightButtonText = "Retry",
				RightButtonCallback = () =>
				{
					_ = SoundManager.Play(CommonSounds.Instance.Click01);
					CreateAccount();
				},
			});
		}

		#region Button Handlers

		private void OnBackBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			lobbyDialogue.ShowLoginScreen();
		}

		private void OnCreateBtn()
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
