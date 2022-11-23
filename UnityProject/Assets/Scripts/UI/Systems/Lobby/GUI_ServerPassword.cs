using Core.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class GUI_ServerPassword : MonoBehaviour
	{
		[SerializeField]
		private InputField passwordInputField = null;

		public void OnJoinBtn()
		{
			var authenticator = CustomNetworkManager.Instance.authenticator as Authenticator;

			if (authenticator == null)
			{
				Logger.LogError("Authenticator wrong type?");
				return;
			}

			authenticator.ClientSendPassword(passwordInputField.text);
			gameObject.SetActive(false);
		}
	}
}