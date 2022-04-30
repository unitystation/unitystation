using UnityEngine;
using UnityEngine.UI;
using UI.CharacterCreator;

namespace Lobby
{
	public class LobbyManager : MonoBehaviour
	{
		public static LobbyManager Instance;
		public AccountLogin accountLogin;
		public CharacterCustomization characterCustomization;

		public GUI_LobbyDialogue lobbyDialogue;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(this);
			}
		}

		private void Start()
		{
			DetermineUIScale();
			UIManager.Display.SetScreenForLobby();
			EventManager.AddHandler(Event.LoggedOut, SetOnLogOut);
		}

		private void OnEnable()
		{
			CustomNetworkManager.Instance.OnClientDisconnected.AddListener(OnClientDisconnect);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.LoggedOut, SetOnLogOut);
			CustomNetworkManager.Instance?.OnClientDisconnected?.RemoveListener(OnClientDisconnect);
		}

		public void OnClientDisconnect()
		{
			lobbyDialogue.OnClientDisconnect();
		}

		private void DetermineUIScale()
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
