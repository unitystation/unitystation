using Audio.Containers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Globalization;

/// <summary>
/// Manages the UI buttons for letting the player choose their desired job.
/// </summary>
public class GUI_PlayerJobs : MonoBehaviour
{
	public GameObject buttonPrefab;
	private CustomNetworkManager networkManager;

	/// <summary>
	/// The gameobject displaying the various job selection buttons.
	/// </summary>
	public GameObject screen_Jobs = null;

	/// <summary>
	/// The window showing information about a job.
	/// </summary>
	public GUI_JobInfo jobInfo = null;
	/// <summary>
	/// The gameobject to display the spectate button and others
	/// </summary>
	public GameObject footer = null;

	/// <summary>
	/// A gameobject that is shown after job selection when the player is waiting to spawn.
	/// </summary>
	[SerializeField]
	[Tooltip("Number of seconds to wait after selecting a job. If the player does not spawn within that time the job selection re-opens.")]
	private GameObject waitMessage = null;

	/// <summary>
	/// After the player selects a job this timer will be used to keep track of how long they've waited.
	/// When it is above 0 the timer will run and wait for the player to spawn.
	/// </summary>
	private float waitForSpawnTimer = 0;

	/// <summary>
	/// Number of seconds to wait after selecting a job. If the player does not spawn within that time the job selection re-opens.
	/// </summary>
	[SerializeField]
	[Range(0, 15)]
	[Tooltip("Number of seconds to wait after selecting a job. If the player does not spawn within that time the job selection re-opens.")]
	private float waitForSpawnTimerMax = 6;

	/// <summary>
	/// Called when the player select a job selection button.
	/// Assigns the player that job and spawns them, unless the job was already taken.
	/// </summary>
	/// <param name="preference">The job associated with the button.</param>
	private void BtnOk(JobType preference)
	{
		if (waitForSpawnTimer > 0)
		{
			return; // Disallowing picking a job while another job has been selected.
		}
		SoundManager.Play("Click01");
		screen_Jobs.SetActive(false);
		footer.SetActive(false);
		waitMessage.SetActive(true);

		PlayerManager.LocalViewerScript.RequestJob(preference);
		waitForSpawnTimer = waitForSpawnTimerMax;
	}

	void OnEnable()
	{
		screen_Jobs.SetActive(true);
		SetFooter();
		footer.SetActive(true);

	}

	/// <summary>
	/// If a role has been selected this waits for the player to spawn.
	/// </summary>
	private void Update()
	{
		if (PlayerManager.HasSpawned)
		{
			// Job selection is finished, close the window.
			waitForSpawnTimer = 0;
			MusicManager.SongTracker.Stop();
			gameObject.SetActive(false);
			waitMessage.SetActive(false);
			screen_Jobs.SetActive(true);
			footer.SetActive(true);
		}

		if (waitForSpawnTimer > 0)
		{
			waitForSpawnTimer -= Mathf.Max(0, Time.deltaTime);
			if (waitForSpawnTimer <= 0)
			{
				// Job selection failed, re-open it.
				SoundManager.Play("Click01");
				screen_Jobs.SetActive(true);
				footer.SetActive(true);
				waitMessage.SetActive(false);
			}
		}
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

			// This line was added for unit testing - but now it's only rewrite occupations meta
			//occupation.name = jobType.ToString();

			var image = occupationGO.GetComponent<Image>();
			var text = occupationGO.GetComponentInChildren<TextMeshProUGUI>();

			image.color = occupation.ChoiceColor;
			text.text = occupation.DisplayName + " (" + active + " of " + available + ")";
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

			var check = PlayerList.Instance.ClientCheckBanReturn(occupation.JobType);

			if (check != null)
			{
				var entryTime = DateTime.ParseExact(check.dateTimeOfBan,"O",CultureInfo.InvariantCulture);
				var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);

				image.color = Color.red;
				var msg = check.isPerma ? "Perma Banned" : $"banned for {Mathf.RoundToInt((float)check.minutes - totalMins)} minutes";
				text.text = occupation.DisplayName + $" is {msg}";

				occupationGO.GetComponent<Button>().interactable = false;
			}

			// Job window listener
			Occupation occupationOfTrigger = occupation;
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerEnter;
			entry.callback.AddListener((eventData) => { jobInfo.Job = occupationOfTrigger; });
			occupationGO.GetComponent<EventTrigger>().triggers.Add(entry);

			occupationGO.SetActive(true);
		}


		screen_Jobs.SetActive(true);
	}
	/// <summary>
	/// Code for loading the footer, currently only containing a spectate button
	/// </summary>
	public void SetFooter()
	{
		GameObject occupationGO = Instantiate(buttonPrefab, footer.transform);
		occupationGO.GetComponent<Image>().color = Color.white;
		occupationGO.GetComponentInChildren<TextMeshProUGUI>().text = "Spectate";
		occupationGO.transform.localScale = new Vector3(1.0f, 1f, 1.0f);
		occupationGO.GetComponent<Button>().onClick.AddListener(() => { PlayerManager.LocalViewerScript.Spectate(); });

	}
}