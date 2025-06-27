using System;
using Core.Networking;
using Logs;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class GUI_ServerPassword : MonoBehaviour
	{
		[SerializeField]
		private InputField passwordInputField = null;

		public bool Haspass = false;

		public void OnEnable()
		{
			Haspass = true;
		}

		public void OnJoinBtn()
		{
			var authenticator = CustomNetworkManager.Instance.authenticator as Authenticator;

			if (authenticator == null)
			{
				Loggy.Error("Authenticator wrong type?");
				return;
			}

			authenticator.ClientSendLobbyPassword(passwordInputField.text);
		}
	}
}
