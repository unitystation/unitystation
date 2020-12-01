using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Server;
using Messages.Client;
using Systems.GhostRoles;

namespace UI.Windows
{
	/// <summary>
	/// A window available to ghosts that displays a list of available ghost roles
	/// (as <see cref="GhostRoleWindowEntry"/> and enables requesting assignment of said roles.
	/// </summary>
	public class GhostRoleWindow : MonoBehaviour
	{
		[SerializeField]
		private GameObject ghostRoleEntryPrefab = default;
		[SerializeField]
		private Transform listContainer = default;
		[SerializeField]
		private GameObject noRolesLabel = default;

		private readonly Dictionary<uint, GhostRoleWindowEntry> entries = new Dictionary<uint, GhostRoleWindowEntry>();

		private void OnEnable()
		{
			RequestAvailableGhostRolesMessage.SendMessage();
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
			if (entries.ContainsKey(key) == false) return;

			entries[key].SetResponseMessage(responseCode);
		}

		private void UpdateNoRolesLabel()
		{
			noRolesLabel.SetActive(entries.Count < 1);
		}
	}
}
