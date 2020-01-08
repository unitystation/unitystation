using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GUI_PlayerJobs : MonoBehaviour
{
	public GameObject buttonPrefab;
	private CustomNetworkManager networkManager;
	public GameObject screen_Jobs;

	private void BtnOk(JobType preference)
	{
		SoundManager.Play("Click01");
		PlayerManager.LocalViewerScript.CmdRequestJob(preference, PlayerManager.CurrentCharacterSettings);
		SoundManager.SongTracker.Stop();
		// Close this window
		gameObject.SetActive(false);
	}

	public void UpdateJobsList()
	{
		screen_Jobs.SetActive(false);

		foreach (Transform child in screen_Jobs.transform)
		{
			Destroy(child.gameObject);
		}

		foreach (Occupation occupation in OccupationList.Instance.Occupations)
		{
			JobType jobType = occupation.JobType;

			//NOTE: Commenting this out because it can actually be changed just by editing allowed occupation list,
			//doesn't need manual removal and this allows direct spawning as syndie for testing just by adding them
			//to that list
			// For nuke ops mode, syndis spawn via a different button
			// if (jobType == JobType.SYNDICATE)
			// {
			// 	continue;
			// }

			int active = GameManager.Instance.GetOccupationsCount(jobType);
			int available = GameManager.Instance.GetOccupationMaxCount(jobType);

			GameObject occupationGO = Instantiate(buttonPrefab, screen_Jobs.transform);

			occupation.name = jobType.ToString();
			var color = occupation.ChoiceColor;

			occupationGO.GetComponent<Image>().color = color;
			occupationGO.GetComponentInChildren<Text>().text = occupation.DisplayName + " (" + active + " of " + available + ")";
			occupationGO.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

			// Disabled button for full jobs
			if (active >= available)
			{
				occupationGO.GetComponentInChildren<Button>().interactable = false;
			}
			else // Enabled button with listener for vacant jobs
			{
				occupationGO.GetComponent<Button>().onClick.AddListener(() => { BtnOk(jobType); });
			}

			occupationGO.SetActive(true);
		}
		screen_Jobs.SetActive(true);
	}
}