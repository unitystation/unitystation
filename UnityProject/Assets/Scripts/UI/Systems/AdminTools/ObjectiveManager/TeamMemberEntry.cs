using AdminTools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamMemberEntry : MonoBehaviour
{
	[SerializeField]
	private TMP_Text text;

	private bool isNew = false;

	private TeamMemberInfo teamMemberInfo;
	public TeamMemberInfo TeamMemberInfo => teamMemberInfo;

	private AdminTools.AdminPlayerEntry adminInfo;
	public AdminTools.AdminPlayerEntry AdminInfo => adminInfo;

	private TeamObjectiveAdminPage teamObjectiveAdminPage;
	[SerializeField]
	private Text removeButtonText;

	public void Init(TeamObjectiveAdminPage teamObjectiveAdminPageToSet, TeamMemberInfo teamMemberInformation, AdminTools.AdminPlayerEntry adminInfoToSet)
	{
		teamObjectiveAdminPage = teamObjectiveAdminPageToSet;
		adminInfo = adminInfoToSet;
		teamMemberInfo = teamMemberInformation;
		text.text = $"{adminInfo.PlayerData.name}: {adminInfo.PlayerData.currentJob}";
	}

	public void RemoveMember()
	{
		if (isNew == true)
		{
			teamObjectiveAdminPage.RemoveMemberEntry(this);
		} else
		{
			teamMemberInfo.isToRemove = !teamMemberInfo.isToRemove;
			if (teamMemberInfo.isToRemove == true)
			{
				text.fontStyle = FontStyles.Strikethrough;
				removeButtonText.color = new Color(1, 0, 0, 1);
			} else
			{
				text.fontStyle = FontStyles.Normal;
				removeButtonText.color = new Color(0.8f, 0.2f, 0.2f, 1);
			}
		}
	}

	public void Init(TeamObjectiveAdminPage teamObjectiveAdminPageToSet, AdminPlayerEntry adminInfoToSet)
	{
		isNew = true;
		teamObjectiveAdminPage = teamObjectiveAdminPageToSet;
		teamMemberInfo = new TeamMemberInfo()
		{
			Id = adminInfoToSet.PlayerData.uid
		};
		adminInfo = adminInfoToSet;
		text.text = $"{adminInfo.displayName} {adminInfo.PlayerData.currentJob}";
	}
}
