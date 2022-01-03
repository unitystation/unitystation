using System;
using System.Collections.Generic;
using DatabaseAPI;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class AccountLogin : MonoBehaviour
	{
		//Account login screen:
		public InputField userNameInput;
		public InputField passwordInput;

		private void Start()
		{
			if (PlayerPrefs.HasKey("lastLogin"))
			{
				userNameInput.text = PlayerPrefs.GetString("lastLogin");
			}
		}

		public async void TryLogin(Action<string> successAction, Action<string> errorAction)
		{
			await FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(userNameInput.text,
				passwordInput.text).ContinueWithOnMainThread(async task =>
			{
				if (task.IsCanceled)
				{
					Logger.LogError($"Sign in error: {task.Exception.Message}", Category.DatabaseAPI);
					errorAction?.Invoke(task.Exception.Message);
					passwordInput.text = "";
					return;
				}

				if (task.IsFaulted)
				{
					Logger.LogError($"Sign in error: {task.Exception.Message}", Category.DatabaseAPI);
					errorAction?.Invoke(task.Exception.Message);
					passwordInput.text = "";
					return;
				}

				await ServerData.ValidateUser(task.Result, successAction, errorAction);
				passwordInput.text = "";
			});
		}

		public bool ValidLogin()
		{
			//Missing username or password
			if (string.IsNullOrEmpty(userNameInput.text) || string.IsNullOrEmpty(passwordInput.text))
			{
				return false;
			}

			return true;
		}
	}
}
