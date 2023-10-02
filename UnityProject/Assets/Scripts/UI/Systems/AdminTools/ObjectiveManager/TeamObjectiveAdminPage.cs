using AdminTools;
using Antagonists;
using Logs;
using Messages.Client.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamObjectiveAdminPage : MonoBehaviour
{
	[SerializeField]
	private TeamObjectiveEntry entryObj;
	[SerializeField]
	private GameObject contentObjs;
	[SerializeField]
	private GameObject objectiveAddEntry;
	[SerializeField]
	private Dropdown objectiveDropdown;
	private readonly Dictionary<int, Objective> objectiveDropdownDictionary = new ();

	[SerializeField]
	private TeamMemberEntry entryMember;
	[SerializeField]
	private GameObject contentMember;
	[SerializeField]
	private GameObject playersAddEntry;
	[SerializeField]
	private Dropdown playersDropdown;

	[SerializeField]
	private TeamEntry entryTeam;
	[SerializeField]
	private GameObject contentTeams;
	[SerializeField]
	private TMP_Text teamName;
	[SerializeField]
	private GameObject teamsAddEntry;

	private readonly Dictionary <int, AdminPlayerEntry> playersDropdownDictionary = new ();

	private readonly List<TeamEntry> entryTeams = new List<TeamEntry>();
	private readonly List<TeamObjectiveEntry> entryObjs = new List<TeamObjectiveEntry>();
	private readonly List<TeamMemberEntry> entryMembers = new List<TeamMemberEntry>();
	private TeamEntry currentTeam;
	private TeamObjectiveEntry currentObjective;

	private GUI_AdminTools gUI_AdminTools;

	[SerializeField]
	private GameObject objectiveSettings;
	[SerializeField]
	private GameObject objectiveList;

	[SerializeField] private ObjectiveAttributesEntry attributesEntry;
	[SerializeField] private GameObject attributesContentArea;
	private readonly List<ObjectiveAttributesEntry> addedAttributesEntries = new List<ObjectiveAttributesEntry>();

	/// <summary>
	/// Open TeamObjectiveAdminPage page
	/// </summary>
	/// <param name="gUI_AdminToolsToSet"></param>
	public void Init(AdminTools.GUI_AdminTools gUI_AdminToolsToSet)
	{
		RequestTeamObjectiveAdminPageRefreshMessage.Send();
		gUI_AdminTools = gUI_AdminToolsToSet;
	}

	/// <summary>
	/// Updating information
	/// </summary>
	/// <param name="info"></param>
	public void RefreshInformation(TeamsInfo info)
	{
		foreach (var x in entryTeams)
		{
			Destroy(x.gameObject);
		}
		entryTeams.Clear();

		foreach (var x in info.TeamsInfos)
		{
			entryTeam.SetActive(true);
			var newEntry = Instantiate(entryTeam.gameObject, contentTeams.transform).GetComponent<TeamEntry>();
			entryTeam.SetActive(false);
			newEntry.Init(this, x);
			entryTeams.Add(newEntry);
		}
		currentTeam = null;
		CloseSettingsTab();

		DisplayTeamInfo(entryTeams.FirstOrDefault());
		entryTeam.SetActive(false);
		teamsAddEntry.transform.SetAsLastSibling();
	}

	/// <summary>
	/// Display selected team information
	/// </summary>
	/// <param name="entry"></param>
	public void DisplayTeamInfo(TeamEntry entry)
	{
		if (currentTeam != null)
			entry.UpdateTeamInfo(entryObjs, entryMembers);

		foreach (var x in entryObjs)
		{
			Destroy(x.gameObject);
		}
		entryObjs.Clear();
		foreach (var x in entryMembers)
		{
			Destroy(x.gameObject);
		}
		entryMembers.Clear();
		entryMember.SetActive(false);
		entryObj.SetActive(false);

		if (entry == null)
			return;

		currentTeam = entry;
		RefreshPlayers(entry);
		RefreshObjectives(entry);

		teamName.text = entry.TeamInfo.Name;
		foreach (var x in entry.TeamInfo.ObjsInfo)
		{
			entryObj.SetActive(true);
			var newEntry = Instantiate(entryObj.gameObject, contentObjs.transform).GetComponent<TeamObjectiveEntry>();
			entryObj.SetActive(false);
			newEntry.Init(this, x);
			entryObjs.Add(newEntry);
		}

		foreach (var x in entry.TeamInfo.MembersInfo)
		{
			entryMember.SetActive(true);
			var newEntry = Instantiate(entryMember.gameObject, contentMember.transform).GetComponent<TeamMemberEntry>();
			entryMember.SetActive(false);

			foreach (var adminInfo in gUI_AdminTools.GetPlayerEntries())
			{
				if (adminInfo.PlayerData.uid == x.Id)
				{
					newEntry.Init(this, x, adminInfo);
					break;
				}
			}
			entryMembers.Add(newEntry);
		}

		objectiveAddEntry.SetActive(entry.TeamData.CanBeAddedNewObjectives);
		playersAddEntry.SetActive(entry.TeamData.CanBeAddedNewMembers);

		playersAddEntry.transform.SetAsLastSibling();
		objectiveAddEntry.transform.SetAsLastSibling();
	}

	/// <summary>
	/// Removes TeamObjectiveEntry from page
	/// </summary>
	/// <param name="teamObjectiveEntry"></param>
	public void RemoveObjectiveEntry(TeamObjectiveEntry teamObjectiveEntry)
	{
		entryObjs.Remove(teamObjectiveEntry);
		Destroy(teamObjectiveEntry.gameObject);
	}

	/// <summary>
	/// Removes TeamMemberEntry from page
	/// </summary>
	/// <param name="teamMemberEntry"></param>
	public void RemoveMemberEntry(TeamMemberEntry teamMemberEntry)
	{
		entryMembers.Remove(teamMemberEntry);
		Destroy(teamMemberEntry.gameObject);
	}

	/// <summary>
	/// Display objectives that team contains
	/// </summary>
	/// <param name="entry"></param>
	private void RefreshObjectives(TeamEntry entry)
	{
		var team = entry.TeamData;
		var objList = new List<Dropdown.OptionData>();
		objectiveDropdown.ClearOptions();
		objectiveDropdownDictionary.Clear();

		for (int i = 0; i < team.CoreObjectives.Count(); i++)
		{
			var x = team.CoreObjectives.ElementAt(i);

			objList.Add(new Dropdown.OptionData(x.ObjectiveName));
			objectiveDropdownDictionary.Add(i, x);
		}

		if (objList.Count == 0 || team.CanBeAddedNewObjectives == false)
		{
			objectiveAddEntry.SetActive(false);
			return;
		}
		objectiveAddEntry.SetActive(true);

		objectiveDropdown.AddOptions(objList);
		objectiveDropdown.value = 0;
	}


	/// <summary>
	/// Display players that team contains
	/// </summary>
	/// <param name="entry"></param>
	public void RefreshPlayers(TeamEntry entry)
	{
		var team = entry.TeamData;
		var players = new List<Dropdown.OptionData>();
		playersDropdown.ClearOptions();
		List<AdminPlayerEntry> playerList = gUI_AdminTools.GetPlayerEntries();
		playersDropdownDictionary.Clear();
		for (int i = 0; i < playerList.Count; i++)
		{
			AdminPlayerEntry x = playerList[i];
			bool playerInTeam = false;
			foreach (var member in entryMembers)
			{
				if (member.AdminInfo.PlayerData.uid == x.PlayerData.uid)
				{
					playerInTeam = true;
					break;
				}
			}
			if (playerInTeam == true)
				continue;
			players.Add(new Dropdown.OptionData(x.PlayerData.name));
			playersDropdownDictionary.Add(i, x);
		}

		if (players.Count == 0 || team.CanBeAddedNewMembers == false)
		{
			playersAddEntry.SetActive(false);
			return;
		}
		playersAddEntry.SetActive(true);

		playersDropdown.AddOptions(players);
		playersDropdown.value = 0;
	}

	/// <summary>
	/// Opens objective settings
	/// </summary>
	/// <param name="objective"></param>
	public void OpenSettingsTab(TeamObjectiveEntry objective)
	{
		if (currentObjective != null)
			CloseSettingsTab();
		objectiveSettings.SetActive(true);
		objectiveList.SetActive(false);
		currentObjective = objective;

		var selectedObjective = AntagData.Instance.FromIndexObj(objective.Info.PrefabID);

		attributesContentArea.SetActive(selectedObjective.attributes.Count != 0);

		foreach (var x in addedAttributesEntries)
		{
			Destroy(x.gameObject);
		}
		addedAttributesEntries.Clear();

		foreach (var x in selectedObjective.attributes)
		{
			attributesEntry.SetActive(true);
			var newEntry = Instantiate(attributesEntry, attributesContentArea.transform).GetComponent<ObjectiveAttributesEntry>();
			attributesEntry.SetActive(false);
			newEntry.Init(gUI_AdminTools, x, selectedObjective);
			addedAttributesEntries.Add(newEntry);
		}

		attributesEntry.SetActive(false);
	}

	/// <summary>
	/// Close objective settings
	/// </summary>
	public void CloseSettingsTab()
	{
		if (currentObjective != null)
		{
			var addedAtributes = new List<ObjectiveAttribute>();
			foreach (var x in addedAttributesEntries)
			{
				addedAtributes.Add(x.Attribute);
			}

			currentObjective.Info.Attributes = addedAtributes;
		}
		currentObjective = null;
		objectiveSettings.SetActive(false);
		objectiveList.SetActive(true);
	}

	/// <summary>
	/// Adds new member to team
	/// </summary>
	public void AddNewEntryMember()
	{
		entryMember.SetActive(true);
		var newEntry = Instantiate(entryMember.gameObject, contentMember.transform).GetComponent<TeamMemberEntry>();
		entryMember.SetActive(false);
		newEntry.Init(this, playersDropdownDictionary[playersDropdown.value]);
		entryMembers.Add(newEntry);
		playersAddEntry.transform.SetAsLastSibling();
		RefreshPlayers(currentTeam);
	}

	/// <summary>
	/// Adds new objective to team
	/// </summary>
	public void AddNewEntryObj()
	{
		entryObj.SetActive(true);
		var newEntry = Instantiate(entryObj.gameObject, contentObjs.transform).GetComponent<TeamObjectiveEntry>();
		entryObj.SetActive(false);
		newEntry.Init(this, objectiveDropdownDictionary[objectiveDropdown.value]);
		entryObjs.Add(newEntry);
		objectiveAddEntry.transform.SetAsLastSibling();
	}

	/// <summary>
	/// Sends changed information to server
	/// </summary>
	public void SendToServer()
	{
		CloseSettingsTab();
		currentTeam.UpdateTeamInfo(entryObjs, entryMembers);

		var teams = new List<TeamInfo>();
		foreach (var x in entryTeams)
		{
			teams.Add(x.TeamInfo);
		}
		RequestAdminTeamUpdateMessage.Send(teams);
		// Sending message to refresh team list
		RequestTeamObjectiveAdminPageRefreshMessage.Send();
	}

	#region ServerPart

	/// <summary>
	/// Server side. Updates server teams
	/// </summary>
	/// <param name="generalInfo"></param>
	public static void ProcessServer(TeamsInfo generalInfo)
	{
		foreach (var teamInf in generalInfo.TeamsInfos)
		{
			var serverTeam = AntagManager.Instance.Teams[teamInf.ID];

			var wasAddedNewObj = false;
			foreach (var objInfo in teamInf.ObjsInfo)
			{
				var isNew = true;
				foreach (var originalObj in serverTeam.TeamObjectives)
				{
					if (originalObj.ID == objInfo.ID)
					{
						isNew = false;
					}
				}
				if (isNew == true)
				{
					try
					{
						AddObjective(serverTeam, objInfo);
						wasAddedNewObj = true;
					}
					catch (Exception ex)
					{
						Loggy.LogError($"[TeamObjectiveAdminPage/ProcessServer] Failed to add objective to team\n {ex}");
					}
				} else if (objInfo.toDelete)
				{
					RemoveObjective(serverTeam, objInfo);
					wasAddedNewObj = true;
				}
			}
			if (wasAddedNewObj == true)
			{
				serverTeam.RemindEveryone();
			}

			foreach (var member in teamInf.MembersInfo)
			{
				var isNew = true;
				var player = PlayerList.Instance.GetPlayerByID(member.Id);
				if (player == null)
					continue;

				foreach (var originalMember in serverTeam.TeamMembers)
				{
					if (originalMember.Owner.ControlledBy.UserId == member.Id)
					{
						isNew = false;
					}
				}

				if (isNew)
				{
					AddMember(serverTeam, player);
				} else if (member.isToRemove)
				{
					RemoveMember(serverTeam, player);
				}
			}
		}
	}

	/// <summary>
	/// Server side. Removes member from team
	/// </summary>
	/// <param name="serverTeam"></param>
	/// <param name="member"></param>
	private static void RemoveMember(Team serverTeam, PlayerInfo member)
	{
		serverTeam.RemoveTeamMember(member.Mind);
	}

	/// <summary>
	/// Server side. Adds member to team
	/// </summary>
	/// <param name="serverTeam"></param>
	/// <param name="member"></param>
	private static void AddMember(Team serverTeam, PlayerInfo member)
	{
		serverTeam.AddTeamMember(member.Mind);
	}

	/// <summary>
	/// Server side. Removes objective from team
	/// </summary>
	/// <param name="serverTeam"></param>
	/// <param name="objInfo"></param>
	private static void RemoveObjective(Team serverTeam, ObjectiveInfo objInfo)
	{
		TeamObjective obj = serverTeam.GetObjectiveByID(objInfo.ID);
		if (obj == null)
		{
			return;
		}
		serverTeam.RemoveTeamObjective(obj);
	}

	/// <summary>
	/// Server side. Adds objective to team
	/// </summary>
	/// <param name="team"></param>
	/// <param name="objInfo"></param>
	private static void AddObjective(Team team, ObjectiveInfo objInfo)
	{
		Objective obj = AntagData.Instance.FromIndexObj(objInfo.PrefabID);
		if (obj == null)
		{
			return;
		}
		foreach (var attr in objInfo.Attributes)
		{
			var attribute = obj.attributes[attr.index];
			if (attr is ObjectiveAttributeItem itemSet && attribute is ObjectiveAttributeItem item)
			{
				item.itemID = itemSet.itemID;
			}
			else if (attr is ObjectiveAttributeNumber numbSet && attribute is ObjectiveAttributeNumber numb)
			{
				numb.number = numbSet.number;
			}
			else if (attr is ObjectiveAttributePlayer plSet && attribute is ObjectiveAttributePlayer pl)
			{
				pl.playerID = plSet.playerID;
			}
			else if (attr is ObjectiveAttributeItemTrait itemTraitSet && attribute is ObjectiveAttributeItemTrait itemTrait)
			{
				itemTrait.itemTraitIndex = itemTraitSet.itemTraitIndex;
			}
		}
		if (obj is TeamObjective teamObj)
		{
			teamObj.DoSetup(team);
			team.AddTeamObjective(obj);
		}
	}
	#endregion
}