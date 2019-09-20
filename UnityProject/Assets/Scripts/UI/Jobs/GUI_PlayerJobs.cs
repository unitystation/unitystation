using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GUI_PlayerJobs : MonoBehaviour
{
	public GameObject buttonPrefab;
	public bool hasPickedAJob;

	public bool isUpToDate;
	private CustomNetworkManager networkManager;
	public GameObject screen_Jobs;
	public Text title;

	private void Update()
	{
		//We only want the job selection screen to show up once
		//And only when we've received the connectedPlayers list from the server
		if (CanBeUpdated())
		{
			UpdateJobsList();
		}
	}

	private void BtnOk(JobType preference)
	{
		SoundManager.Play("Click01");
		PlayerManager.LocalViewerScript.CmdRequestJob(preference, PlayerManager.CurrentCharacterSettings);
		UIManager.Instance.GetComponent<ControlDisplays>().jobSelectWindow.SetActive(false);
		hasPickedAJob = true;
	}

	private void UpdateJobsList()
	{
		screen_Jobs.SetActive(false);

		foreach (Transform child in screen_Jobs.transform)
		{
			Destroy(child.gameObject);
		}

		var occupations = GameManager.Instance.Occupations.OrderBy(
			o => Array.IndexOf(JobTypeExtensions.DisplayOrder, o.GetComponent<OccupationRoster>().Type)).ToList();

		foreach (GameObject occupationGo in occupations)
		{
			JobType jobType = occupationGo.GetComponent<OccupationRoster>().Type;

			// For nuke ops mode, syndis spawn via a different button
			if (jobType == JobType.SYNDICATE)
			{
				continue;
			}

			int active = GameManager.Instance.GetOccupationsCount(jobType);
			int available = GameManager.Instance.GetOccupationMaxCount(jobType);

			GameObject occupation = Instantiate(buttonPrefab, screen_Jobs.transform);

			occupation.name = jobType.ToString();
			var color = jobType.GetDisplayColor();

			occupation.GetComponent<Image>().color = color;
			occupation.GetComponentInChildren<Text>().text = jobType.ToDisplayString() + " (" + active + " of " + available + ")";
			occupation.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

			// Disabled button for full jobs
			if (active >= available)
			{
				occupation.GetComponentInChildren<Button>().interactable = false;
			}
			else // Enabled button with listener for vacant jobs
			{
				occupation.GetComponent<Button>().onClick.AddListener(() => { BtnOk(jobType); });
			}

			occupation.SetActive(true);
		}
		screen_Jobs.SetActive(true);
		isUpToDate = true;
	}

	private bool CanBeUpdated()
	{
		if (isUpToDate || hasPickedAJob)
		{
			return false;
		}

//		//nameList is a syncvar with instant sync on join, while connectedPlayers only happens after player has been spawned
//		//We should not show job selection if we haven't received all the connectedPlayers GOs
//		if (PlayerList.Instance.nameList.Count != PlayerList.Instance.PlayerCount)
//		{
//			return false;
//		}

		return true;
	}
}