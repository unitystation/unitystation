using Messages.Server.AdminTools;
using Mirror;
using ScriptableObjects;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Systems.GhostRoles;
using UnityEngine;

namespace AdminTools
{
	public class GhostRoleAdminPage : MonoBehaviour
	{
		[SerializeField] private GhostRoleAdminEntry entry;
		[SerializeField] private GameObject addButton;
		[SerializeField] private GameObject contentArea;
		private readonly List<GhostRoleAdminEntry> addedEntries = new List<GhostRoleAdminEntry>();
		private readonly Dictionary<int, (GhostRoleClient, GhostRoleAdminEntry)> currentGhostRoles = new ();

		public void Init()
		{
			RefreshInformation();
		}

		public void OnDisable()
		{
			GhostRoleManager.Instance.ClientUpdatedRole.RemoveListener(RefreshEntry);
		}

		public void ConfirmEditing()
		{
			var infoToSend = new List<GhostRoleInfo>();
			foreach (var x in addedEntries)
			{
				infoToSend.Add(x.RoleInfo);
			}
			RefreshInformation();

			RequestAdminGhostRoleUpdateMessage.Send(infoToSend);
		}

		public void CancelEditing()
		{
			RefreshInformation();
		}

		public void RefreshInformation()
		{
			GhostRoleManager.Instance.ClientUpdatedRole.RemoveListener(RefreshEntry);
			foreach (var x in addedEntries)
			{
				Destroy(x.gameObject);
			}
			addedEntries.Clear();
			currentGhostRoles.Clear();

			foreach (KeyValuePair<uint, GhostRoleClient> role in GhostRoleManager.Instance.clientAvailableRoles)
			{
				entry.SetActive(true);
				var newEntry = Instantiate(entry, contentArea.transform).GetComponent<GhostRoleAdminEntry>();
				entry.SetActive(false);
				newEntry.Init(this, role);
				addedEntries.Add(newEntry);
				currentGhostRoles.Add((int)role.Key, (role.Value, newEntry));
			}
			entry.SetActive(false);
			GhostRoleManager.Instance.ClientUpdatedRole.AddListener(RefreshEntry);
			addButton.transform.SetAsLastSibling();
		}

		/// <summary>
		/// Updates entry for given role
		/// </summary>
		/// <param name="role"></param>
		private void RefreshEntry(GhostRoleClient role)
		{
			if (currentGhostRoles.TryGetValue((int)role.RoleKey, out var info))
			{
				if (GhostRoleManager.Instance.clientAvailableRoles.TryGetValue(role.RoleKey, out _))
				{
					// it was updated
					info.Item2.UpdateRole(role);
				}
				else
				{
					// it was deleted
					RemoveEntry(info.Item2);
				}
			} else
			{
				entry.SetActive(true);
				var newEntry = Instantiate(entry, contentArea.transform).GetComponent<GhostRoleAdminEntry>();
				entry.SetActive(false);
				newEntry.Init(this, new KeyValuePair<uint, GhostRoleClient>((uint)role.RoleKey, role));
				addedEntries.Add(newEntry);
				currentGhostRoles.Add((int)role.RoleKey, (role, newEntry));
				addButton.transform.SetAsLastSibling();
			}
		}

		/// <summary>
		/// Adding new ghost role
		/// </summary>
		public void AddNewRole()
		{
			entry.SetActive(true);
			var newEntry = Instantiate(entry, contentArea.transform).GetComponent<GhostRoleAdminEntry>();
			entry.SetActive(false);
			newEntry.Init(this, GhostRoleList.Instance.GhostRoles.ElementAt(5));
			addedEntries.Add(newEntry);
			addButton.transform.SetAsLastSibling();
		}

		/// <summary>
		/// Removing entry by given ghost role entry
		/// </summary>
		/// <param name="ghostRoleAdminEntry"></param>
		public void RemoveEntry(GhostRoleAdminEntry ghostRoleAdminEntry)
		{
			addedEntries.Remove(ghostRoleAdminEntry);
			currentGhostRoles.Remove(ghostRoleAdminEntry.RoleInfo.RoleKey);
			Destroy(ghostRoleAdminEntry.gameObject);
		}

		/// <summary>
		/// Proceed new ghost information on server
		/// </summary>
		/// <param name="information"></param>
		[Server]
		public static void ProceedGhostRolesUpdate(GhostRolesInfo information)
		{
			foreach (var ghostRoleInfo in information.Roles)
			{
				if (ghostRoleInfo.MinPlayers < 0 && ghostRoleInfo.MaxPlayers < ghostRoleInfo.MinPlayers)
					continue;
				if (ghostRoleInfo.IsNew)
				{
					ghostRoleInfo.RoleKey = (int)GhostRoleManager.Instance.ServerCreateRole(GhostRoleList.Instance.FromIndex((short)ghostRoleInfo.RoleIndex));
				} else if (ghostRoleInfo.ToRemove)
				{
					GhostRoleManager.Instance.ServerRemoveRole((uint)ghostRoleInfo.RoleKey);
					continue;
				}
				if (ghostRoleInfo.RoleKey < 0)
					continue;
				GhostRoleManager.Instance.ServerUpdateRole((uint)ghostRoleInfo.RoleKey, ghostRoleInfo.MinPlayers, ghostRoleInfo.MaxPlayers, ghostRoleInfo.Timeout, ghostRoleInfo.RoleIndex);
			}
		}
	}
}