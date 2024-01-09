using Antagonists;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamEntry : MonoBehaviour
{
	private TeamInfo teamInfo;
	public TeamInfo TeamInfo => teamInfo;

	private TeamData teamData;
	public TeamData TeamData => teamData;

	[SerializeField]
	private Text teamName;
	private TeamObjectiveAdminPage teamObjectiveAdminPage;

	public void Init(TeamObjectiveAdminPage teamObjectiveAdminPageToSet, TeamInfo newData)
	{
		teamName.text = newData.Name;
		teamInfo = newData;
		teamData = AntagData.Instance.GetFromIndex((short)newData.Index);
		teamObjectiveAdminPage = teamObjectiveAdminPageToSet;
	}

	public void OnPress()
	{
		teamObjectiveAdminPage.DisplayTeamInfo(this);
	}

	public void UpdateTeamInfo(List<TeamObjectiveEntry> objectiveEntries, List<TeamMemberEntry> teamMembers)
	{
		TeamInfo.MembersInfo.Clear();
		TeamInfo.ObjsInfo.Clear();

		foreach (var member in teamMembers)
		{
			TeamInfo.MembersInfo.Add(member.TeamMemberInfo);
		}

		foreach (var objective in objectiveEntries)
		{
			TeamInfo.ObjsInfo.Add(objective.Info);
		}
	}
}
