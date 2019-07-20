using System;
using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class AccountLogin : MonoBehaviour
	{
		//Account login screen:
		public InputField userNameInput;
		public InputField passwordInput;
		public Toggle autoLoginToggle;

		void Start()
		{
			if (PlayerPrefs.HasKey("lastLogin"))
			{
				userNameInput.text = PlayerPrefs.GetString("lastLogin");
			}
		}
		public void TryLogin(Action<string> successAction, Action<string> errorAction)
		{
			ServerData.AttemptLogin(userNameInput.text, passwordInput.text,
				successAction, errorAction);

			PlayerPrefs.SetString("lastLogin", userNameInput.text);
			PlayerPrefs.Save();
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