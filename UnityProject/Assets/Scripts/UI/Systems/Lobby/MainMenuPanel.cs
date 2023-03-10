﻿using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	/// <summary>
	/// Scripting for the main menu panel found in the lobby UI.
	/// </summary>
	public class MainMenuPanel : MonoBehaviour
	{
		[SerializeField]
		private Button joinButton = default;
		[SerializeField]
		private Button hostButton = default;
		[SerializeField]
		private Button controlInfoButton = default;
		[SerializeField]
		private Button logoutButton = default;
		[SerializeField]
		private Button exitButton = default;

		[SerializeField]
		private Text signedInAsText = default;

		private void Awake()
		{
			joinButton.onClick.AddListener(OnJoinBtn);
			hostButton.onClick.AddListener(OnHostBtn);
			controlInfoButton.onClick.AddListener(OnControlInfoBtn);
			logoutButton.onClick.AddListener(OnLogoutBtn);
			exitButton.onClick.AddListener(OnExitBtn);
		}

		private void OnEnable()
		{
			SetSignedInText();
			joinButton.Select();
		}

		private void SetSignedInText()
		{
			// If false, the main menu panel GameObject was probably set active in the prefab
			if (PlayerManager.Account.IsAvailable)
			{
				signedInAsText.text = $"Logged in as {PlayerManager.Account.Username}";
			}
		}

		private void OnJoinBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.UI.ShowJoinPanel();
		}

		private void OnHostBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.Instance.HostServer();
		}

		private void OnControlInfoBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.UI.ShowControlInformationPanel();
		}

		private void OnLogoutBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.Instance.Logout();
		}

		private void OnExitBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.Instance.Quit();
		}
	}
}
