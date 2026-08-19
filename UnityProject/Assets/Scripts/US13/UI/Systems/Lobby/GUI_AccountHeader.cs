using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Core.Addressables;
using US13.Managers;
using US13.Managers.LobbyManager;
using US13.Player;
using US13.Systems.GameModes;

namespace US13.UI.Systems.Lobby
{
	public class GUI_AccountHeader : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text signedInAsText = default;
		[SerializeField]
		private Button accountButton = default;
		[SerializeField]
		private GameObject dropdown = default;
		[SerializeField]
		private Button characterEditorButton = default;
		[SerializeField]
		private Button mapEditorButton = default;
		[SerializeField]
		private Button dropdownCloseCatcher = default;

		[SerializeField]
		private MapEditor mapMode = default;

		private void Awake()
		{
			accountButton.onClick.AddListener(OnAccountBtn);
			characterEditorButton.onClick.AddListener(OnCharacterEditorBtn);
			mapEditorButton.onClick.AddListener(OnMapEditorBtn);
			dropdownCloseCatcher.onClick.AddListener(CloseDropdown);
		}

		private void OnEnable()
		{
			SetDropdownOpen(false);
			SetSignedInText();
		}

		private void OnDisable()
		{
			SetDropdownOpen(false);
		}

		private void SetSignedInText()
		{
			if (PlayerManager.Account.IsAvailable == false)
			{
				signedInAsText.text = "Offline";
				return;
			}

			signedInAsText.text = $"Logged in as {PlayerManager.Account.Username}";
		}

		private void OnAccountBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			SetDropdownOpen(dropdown.activeSelf == false);
		}

		private void CloseDropdown()
		{
			SetDropdownOpen(false);
		}

		private void SetDropdownOpen(bool open)
		{
			dropdown.SetActive(open);
			dropdownCloseCatcher.gameObject.SetActive(open);
		}

		private void OnCharacterEditorBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			SetDropdownOpen(false);
			LobbyManager.Instance.ShowCharacterEditor();
		}

		private void OnMapEditorBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			SetDropdownOpen(false);
			GameManager.Instance.SetGameMode(mapMode, true);
			GameManager.Instance.SecretGameMode = false;
			LobbyManager.Instance.HostServer();
		}
	}
}
