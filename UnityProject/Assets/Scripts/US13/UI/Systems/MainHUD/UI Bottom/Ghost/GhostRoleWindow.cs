using System.Collections.Generic;
using UnityEngine;
using US13.Managers.UpdateManager;
using US13.Messages.Client.GhostRoles;
using US13.Messages.Server.GhostRoles;
using US13.Systems.GhostRoles;

namespace US13.UI.Systems.MainHUD.UI_Bottom.Ghost
{
	/// <summary>
	/// A window available to ghosts that displays a list of available ghost roles
	/// (as <see cref="GhostRoleWindowEntry"/> and enables requesting assignment of said roles.
	/// </summary>
	public class GhostRoleWindow : MonoBehaviour
	{
		[SerializeField] private GameObject ghostRoleEntryPrefab = default;
		[SerializeField] private Transform listContainer = default;
		[SerializeField] private GameObject noRolesLabel = default;

		private readonly Dictionary<uint, GhostRoleWindowEntry> entries = new Dictionary<uint, GhostRoleWindowEntry>();

		private void OnEnable()
		{
			RequestAvailableGhostRolesMessage.SendMessage();
			UpdateManager.Add(RequestAvailableGhostRolesMessage.SendMessage, 3f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RequestAvailableGhostRolesMessage.SendMessage);
		}

		public void CloseWindow()
		{
			gameObject.SetActive(false);
		}

		public void AddOrUpdateEntry(uint key, GhostRoleClient role)
		{
			if (entries.ContainsKey(key) == false)
			{
				GameObject entry = Instantiate(ghostRoleEntryPrefab, listContainer);
				entries.Add(key, entry.GetComponent<GhostRoleWindowEntry>());
			}

			entries[key].SetValues(key, role);

			UpdateNoRolesLabel();
		}


		public void Cleanup()
		{
			foreach (var Entry in entries)
			{
				Destroy(Entry.Value.gameObject);
			}
			entries.Clear();
			UpdateNoRolesLabel();
		}

		public void RemoveEntry(uint key)
		{
			if (entries.ContainsKey(key))
			{
				Destroy(entries[key].gameObject);
				entries.Remove(key);
			}

			UpdateNoRolesLabel();
		}

		public void DisplayResponseMessage(uint key, GhostRoleResponseCode responseCode)
		{
			if (entries.TryGetValue(key, out var entry) == false) return;
			entry.SetResponseMessage(responseCode);
		}

		private void UpdateNoRolesLabel()
		{
			noRolesLabel.SetActive(entries.Count < 1);
		}
	}
}
