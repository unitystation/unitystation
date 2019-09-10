using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class LobbyManager : MonoBehaviour
	{
		public static LobbyManager Instance;
		public AccountLogin accountLogin;
		public CharacterCustomization characterCustomization;
		public Toggle hostToggle;

		public GUI_LobbyDialogue lobbyDialogue;

		void Awake()
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

		void Start()
		{
			if (BuildPreferences.isForRelease)
			{
				hostToggle.gameObject.SetActive(false);
			}

			DetermineUIScale();
		}

		void DetermineUIScale()
		{
			if (!Application.isMobilePlatform)
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

		void OnEnable()
		{
			EventManager.AddHandler(EVENT.LoggedOut, SetOnLogOut);
		}

		void OnDisable()
		{
			EventManager.RemoveHandler(EVENT.LoggedOut, SetOnLogOut);
		}

		private void SetOnLogOut()
		{
			characterCustomization.gameObject.SetActive(false);
			accountLogin.gameObject.SetActive(true);
			lobbyDialogue.ShowLoginScreen();
		}
	}
}