using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	/// <summary>
	/// Scripting for the server history panel found in the lobby UI.
	/// </summary>
	public class ServerHistoryPanel : MonoBehaviour
	{
		[SerializeField]
		private GameObject historyLogTemplate = default;
		[SerializeField]
		private Transform entriesContainer = default;
		[SerializeField]
		private Button backButton = default;

		private void Awake()
		{
			backButton.onClick.AddListener(OnBackBtn);
		}

		private void OnEnable()
		{
			GenerateEntries();
		}

		private void OnDisable()
		{
			ClearEntries();
		}

		private void OnBackBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.UI.ShowJoinPanel();
		}

		private void GenerateEntries()
		{
			foreach (var entry in LobbyManager.Instance.ServerJoinHistory)
			{
				var newEntry = Instantiate(historyLogTemplate, entriesContainer);
				newEntry.GetComponent<HistoryLogEntry>().SetData(entry);
				newEntry.SetActive(true);
			}
		}

		private void ClearEntries()
		{
			foreach (Transform entry in entriesContainer.transform)
			{
				if (entry.gameObject == historyLogTemplate) continue;

				Destroy(entry.gameObject);
			}
		}
	}
}
