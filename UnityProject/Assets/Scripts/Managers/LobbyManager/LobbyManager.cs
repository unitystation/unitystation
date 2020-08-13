using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class LobbyManager : MonoBehaviourSingleton<LobbyManager>
	{
		public AccountLogin accountLogin;
		public CharacterCustomization characterCustomization;
		public Toggle hostToggle;

		public GUI_LobbyDialogue lobbyDialogue;

		void Start()
		{
			DetermineUIScale();
			UIManager.Display.SetScreenForLobby();
			EventManager.AddHandler(EVENT.LoggedOut, SetOnLogOut);
			CustomNetworkManager.Instance.OnClientDisconnected.AddListener(OnClientDisconnect);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(EVENT.LoggedOut, SetOnLogOut);
			CustomNetworkManager.Instance?.OnClientDisconnected?.RemoveListener(OnClientDisconnect);
		}

		public void OnClientDisconnect()
		{
			lobbyDialogue.OnClientDisconnect();
		}

		void DetermineUIScale()
		{
			if (Application.isMobilePlatform)
			{
				if (!UIManager.IsTablet)
				{
					characterCustomization.transform.localScale *= 1.25f;
					lobbyDialogue.transform.localScale *= 2.0f;
				}
			}
			else
			{
				if (Screen.height > 720f)
				{
					characterCustomization.transform.localScale *= 0.8f;
					lobbyDialogue.transform.localScale *= 0.8f;
				}
				else
				{
					characterCustomization.transform.localScale *= 0.9f;
					lobbyDialogue.transform.localScale *= 0.9f;
				}
			}
		}

		private void SetOnLogOut()
		{
			characterCustomization.gameObject.SetActive(false);
			accountLogin.gameObject.SetActive(true);
			lobbyDialogue.ShowLoginScreen();
		}
	}
}