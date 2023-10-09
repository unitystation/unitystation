using ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Systems.GhostRoles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class GhostRoleAdminEntry : MonoBehaviour
	{
		[SerializeField] private TMP_Text description;
		[SerializeField] private Image icon;
		[SerializeField] private Dropdown dropdown;
		[SerializeField] private GameObject settings;
		[SerializeField] private GameObject mainPage;
		[SerializeField] private Text removeBoxText;

		[SerializeField] TMP_InputField minPlayerCount;
		[SerializeField] TMP_InputField maxPlayerCount;
		[SerializeField] TMP_InputField timeoutInput;
		[SerializeField] TMP_Text roleName;

		private GhostRoleInfo roleInfo;
		public GhostRoleInfo RoleInfo => roleInfo;
		private GhostRoleAdminPage adminPage;

		public void Init(GhostRoleAdminPage ghostRoleAdminPage, KeyValuePair<uint, Systems.GhostRoles.GhostRoleClient> role)
		{
			adminPage = ghostRoleAdminPage;
			roleInfo = new GhostRoleInfo
			{
				RoleIndex = GhostRoleList.Instance.GetIndex(role.Value.RoleData),
				RoleKey = (int)role.Key,
				MinPlayers = role.Value.MinPlayers,
				MaxPlayers = role.Value.MaxPlayers,
				Timeout = role.Value.RoleData.Timeout
			};

			minPlayerCount.text = role.Value.MinPlayers.ToString();
			maxPlayerCount.text = role.Value.MaxPlayers.ToString();
			timeoutInput.text = role.Value.TimeRemaining.ToString();
			description.text = role.Value.RoleData.DescriptionAdmin;

			var options = new List<Dropdown.OptionData>();
			foreach (var x in GhostRoleList.Instance.GhostRoles)
			{
				var option = new Dropdown.OptionData
				{
					text = x.Name
				};
				options.Add(option);
			}
			dropdown.ClearOptions();
			dropdown.AddOptions(options);
			dropdown.value = GhostRoleList.Instance.GetIndex(role.Value.RoleData);
			roleName.text = role.Value.RoleData.Name;
			icon.sprite = role.Value.RoleData.Sprite.Variance[0].Frames[0].sprite;
		}


		public void Init(GhostRoleAdminPage ghostRoleAdminPage, GhostRoleData role)
		{
			adminPage = ghostRoleAdminPage;
			roleInfo = new GhostRoleInfo
			{
				RoleIndex = GhostRoleList.Instance.GetIndex(role),
				RoleKey = -1,
				MinPlayers = role.MinPlayers,
				MaxPlayers = role.MaxPlayers,
				Timeout = role.Timeout,
				IsNew = true
			};

			minPlayerCount.text = role.MinPlayers.ToString();
			maxPlayerCount.text = role.MaxPlayers.ToString();
			timeoutInput.text = role.Timeout.ToString();
			description.text = role.DescriptionAdmin;

			var options = new List<Dropdown.OptionData>();
			foreach (var x in GhostRoleList.Instance.GhostRoles)
			{
				var option = new Dropdown.OptionData
				{
					text = x.Name
				};
				options.Add(option);
			}
			dropdown.ClearOptions();
			dropdown.AddOptions(options);
			dropdown.value = GhostRoleList.Instance.GetIndex(role);
			roleName.text = role.Name;
			roleName.SetActive(false);
			dropdown.SetActive(true);
			icon.sprite = role.Sprite.Variance[0].Frames[0].sprite;
		}

		public void RemoveEntry()
		{
			if (roleInfo.IsNew == false)
			{
				roleInfo.ToRemove = !roleInfo.ToRemove;
				if (roleInfo.ToRemove)
				{
					removeBoxText.color = new Color(1, 0, 0, 1);
				}
				else
				{
					removeBoxText.color = new Color(0.8f, 0.2f, 0.2f, 1);
				}
			} else
			{
				adminPage.RemoveEntry(this);
			}
		}

		public void ChangeRole()
		{
			var roleData = GhostRoleList.Instance.FromIndex((short)dropdown.value);

			description.text = roleData.DescriptionAdmin;
			roleInfo.RoleIndex = GhostRoleList.Instance.GetIndex(roleData);
			icon.sprite = roleData.Sprite.Variance[0].Frames[0].sprite;
		}

		public void UpdateSettings()
		{
			if (int.TryParse(minPlayerCount.text, out var minPlayers))
			{
				roleInfo.MinPlayers = minPlayers;
			}
			if (int.TryParse(maxPlayerCount.text, out var maxPlayers))
			{
				roleInfo.MaxPlayers = maxPlayers;
			}
			if (int.TryParse(timeoutInput.text, out var timeout))
			{
				roleInfo.Timeout = timeout;
			}
		}

		[ContextMenu("TOGGLESETTINGS")]
		public void ToggleSettings()
		{
			settings.SetActive(mainPage.activeSelf);
			mainPage.SetActive(!mainPage.activeSelf);
		}

		public void UpdateRole(GhostRoleClient role)
		{
			roleInfo.RoleIndex = GhostRoleList.Instance.GetIndex(role.RoleData);
			roleInfo.MinPlayers = role.MinPlayers;
			roleInfo.MaxPlayers = role.MaxPlayers;
			roleInfo.Timeout = role.TimeRemaining;


			minPlayerCount.text = role.MinPlayers.ToString();
			maxPlayerCount.text = role.MaxPlayers.ToString();
			timeoutInput.text = role.TimeRemaining.ToString();
			description.text = role.RoleData.DescriptionAdmin;

			var options = new List<Dropdown.OptionData>();
			foreach (var x in GhostRoleList.Instance.GhostRoles)
			{
				var option = new Dropdown.OptionData
				{
					text = x.Name
				};
				options.Add(option);
			}
			dropdown.ClearOptions();
			dropdown.AddOptions(options);
			dropdown.value = GhostRoleList.Instance.GetIndex(role.RoleData);
			icon.sprite = role.RoleData.Sprite.Variance[0].Frames[0].sprite;
		}
	}
}