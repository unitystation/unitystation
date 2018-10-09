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

		void Start(){
			userNameInput.text = "CubanPete";
			passwordInput.text = "cuban123";
		}
		public void TryLogin(Action<string> successAction, Action<string> errorAction, bool autoLoginSetting)
		{
			ServerData.AttemptLogin(userNameInput.text, passwordInput.text,
				successAction, errorAction, autoLoginSetting);

		}

		void Awake()
		{
			LoadSavedPrefs();
		}

		void LoadSavedPrefs(){
			//TODO CHECK FOR AUTO LOGIN!
		}
	}
}