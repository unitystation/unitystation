using Antagonists;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamObjectiveEntry : MonoBehaviour
{
	[SerializeField]
	private TMP_Text text;

	private bool isNew = false;

	private ObjectiveInfo info;
	public ObjectiveInfo Info => info;

	[SerializeField]
	private GameObject settingsButton;
	[SerializeField]
	private Button checkBoxButton;
	[SerializeField]
	private Text removeButtonText;
	[SerializeField]
	private Text checkBoxButtonText;

	TeamObjectiveAdminPage teamObjectiveAdminPage;

	private Objective currentObjective;
	public Objective CurrentObjective => currentObjective;


	public void Init(TeamObjectiveAdminPage teamObjectiveAdminPageToSet, ObjectiveInfo infoToSet)
	{
		info = infoToSet;
		text.text = $"{info.Description}";
		checkBoxButton.interactable = false;
		teamObjectiveAdminPage = teamObjectiveAdminPageToSet;
		UpdateCheckBox();
		settingsButton.SetActive(false);
	}

	public void Init(TeamObjectiveAdminPage teamObjectiveAdminPageToSet, Objective newObjective)
	{
		settingsButton.SetActive(false);
		checkBoxButton.interactable = false;
		isNew = true;
		teamObjectiveAdminPage = teamObjectiveAdminPageToSet;
		info = new ObjectiveInfo()
		{
			PrefabID = AntagData.Instance.GetIndexObj(newObjective)
		};
		text.text = $"{newObjective.ObjectiveName}";
		currentObjective = Instantiate(newObjective);
		Settings();
		UpdateCheckBox();
	}

	private void UpdateCheckBox()
	{
		if (info.PrefabID >= 0 && AntagData.Instance.FromIndexObj(info.PrefabID).IsEndRoundObjective)
		{
			checkBoxButtonText.text = "E";
		} else if (info.Status)
		{
			checkBoxButtonText.text = "X";
		}
		else
		{
			checkBoxButtonText.text = "";
		}
	}

	public void Remove()
	{
		if (isNew == true)
		{
			teamObjectiveAdminPage.RemoveObjectiveEntry(this);
		} else
		{
			info.ToDelete = !info.ToDelete;
			if (info.ToDelete == true)
			{
				removeButtonText.color = new Color(1, 0, 0, 1);
			}
			else
			{
				removeButtonText.color = new Color(0.8f, 0.2f, 0.2f, 1);
			}

		}
	}

	public void Settings()
	{
		teamObjectiveAdminPage.OpenSettingsTab(this);
	}
}
