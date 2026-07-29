using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Core.Addressables;
using US13.Managers;
using US13.Managers.LobbyManager;
using US13.Player;

namespace US13.UI.Systems.Lobby
{
	public class GUI_AccountHeader : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text signedInAsText = default;
		[SerializeField]
		private Button avatarButton = default;
		[SerializeField]
		private GameObject dropdown = default;
		[SerializeField]
		private Button logoutButton = default;
		[SerializeField]
		private GUI_MainMenu mainMenu = default;

		private void Awake()
		{
			avatarButton.onClick.AddListener(OnAvatarBtn);
			logoutButton.onClick.AddListener(OnLogoutBtn);
		}

		private void OnEnable()
		{
			dropdown.SetActive(false);
			SetSignedInText();
		}

		private void SetSignedInText()
		{
			if (PlayerManager.Account.IsAvailable == false) return;

			signedInAsText.text = $"Logged in as {PlayerManager.Account.Username}";
		}

		private void OnAvatarBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			dropdown.SetActive(dropdown.activeSelf == false);
		}

		private void OnLogoutBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			dropdown.SetActive(false);
			if (mainMenu != null)
			{
				mainMenu.HideHome();
			}
			LobbyManager.Instance.Logout();
		}
	}
}
