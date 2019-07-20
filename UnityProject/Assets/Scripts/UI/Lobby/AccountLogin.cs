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
			userNameInput.text = "CubanPete@unitystation.org";
			passwordInput.text = "cuban123";
		}
		public void TryLogin(Action<string> successAction, Action<string> errorAction)
		{
			ServerData.AttemptLogin(userNameInput.text, passwordInput.text,
				successAction, errorAction);

		}
	}
}