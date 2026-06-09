using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Managers.LobbyManager;

namespace US13.UI.Systems.Lobby.ConnectionHistoryLog
{
	/// <summary>
	/// Scripting for an entry in the history panel found in the lobby UI.
	/// </summary>
	public class HistoryLogEntry : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text addressText;

		[SerializeField]
		private Button joinButton;

		private ConnectionHistory entry;

		private void Awake()
		{
			joinButton.onClick.AddListener(OnJoinButton);
		}

		public void SetData(ConnectionHistory entry)
		{
			this.entry = entry;

			addressText.text = $"{entry.Address}:{entry.Port}";
		}

		private void OnJoinButton()
		{
			LobbyManager.Instance.JoinServer(entry.Address, entry.Port);
		}
	}
}
