﻿using DatabaseAPI;
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

		private void SetOnLogOut()
		{
			characterCustomization.gameObject.SetActive(false);
			accountLogin.gameObject.SetActive(true);
			lobbyDialogue.ShowLoginScreen();
		}
	}
}