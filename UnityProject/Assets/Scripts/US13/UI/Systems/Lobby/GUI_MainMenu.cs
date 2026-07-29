using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Core.Addressables;
using US13.Managers;
using US13.Managers.LobbyManager;
using US13.UI.Core.OptionsMenu;

namespace US13.UI.Systems.Lobby
{
	/// <summary>
	/// Root controller for the main menu screen shown before joining or hosting a game.
	/// </summary>
	public class GUI_MainMenu : MonoBehaviour
	{
		[SerializeField]
		private GameObject home = default;

		[SerializeField]
		private Button joinButton = default;
		[SerializeField]
		private Button hostButton = default;
		[SerializeField]
		private Button optionsButton = default;
		[SerializeField]
		private Button exitButton = default;

		[SerializeField]
		private TMP_Text versionLabel = default;

		[SerializeField]
		private TMP_Text stationTimeLabel = default;

		private int lastShownMinute = -1;

		private void Awake()
		{
			home.SetActive(false);
			joinButton.onClick.AddListener(OnJoinBtn);
			hostButton.onClick.AddListener(OnHostBtn);
			optionsButton.onClick.AddListener(OnOptionsBtn);
			exitButton.onClick.AddListener(OnExitBtn);
		}

		public void ShowHome()
		{
			home.SetActive(true);
		}

		public void HideHome()
		{
			home.SetActive(false);
		}

		private void Start()
		{
			if (versionLabel == null) return;

			versionLabel.text = $"VER: {GameData.BuildNumber}";
		}

		private void Update()
		{
			UpdateStationTime();
		}

		private void UpdateStationTime()
		{
			if (stationTimeLabel == null) return;
			if (DateTime.Now.Minute == lastShownMinute) return;
			lastShownMinute = DateTime.Now.Minute;
			stationTimeLabel.text = $"STATION TIME: {DateTime.Now:HH:mm}";
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

		private void OnOptionsBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			OptionsMenu.Instance.Open();
		}

		private void OnExitBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.Instance.Quit();
		}
	}
}
