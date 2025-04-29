using Audio.Containers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using Core.Utils;
using Messages.Server;
using ScriptableObjects.Characters;

namespace UI
{
	/// <summary>
	/// Manages the UI buttons for letting the player choose their desired job.
	/// </summary>
	public class GUI_PlayerJobs : MonoBehaviour
	{
		public GameObject buttonPrefab;

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

		[SerializeField]
		private GameObject errorInfoWindow = null;

		[SerializeField]
		private Text errorReasonText = null;

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

		[SerializeField] private RoundJoinAttributes attributesJoinList;
		[SerializeField] private Toggle expirementalJobsTestToggle;
		[SerializeField] private Transform expiermentalWarning;


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
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			screen_Jobs.SetActive(false);
			footer.SetActive(false);
			waitMessage.SetActive(true);

			PlayerManager.LocalViewerScript.RequestJob(preference);
			waitForSpawnTimer = waitForSpawnTimerMax;
		}

		private void BtnOk(KeyValuePair<int, CharacterAttribute> attribute)
		{
			if (waitForSpawnTimer > 0)
			{
				return; // Disallowing picking a job while another job has been selected.
			}
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			screen_Jobs.SetActive(false);
			footer.SetActive(false);
			waitMessage.SetActive(true);

			PlayerManager.LocalViewerScript.RequestJob(attribute.Key);
			waitForSpawnTimer = waitForSpawnTimerMax;
		}

		private void ShowJobSelection()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			screen_Jobs.SetActive(true);
			footer.SetActive(true);
			waitMessage.SetActive(false);
		}

		public void BackButton()
		{
			this.gameObject.SetActive(false);
			GUI_PreRoundWindow.Instance.gameObject.SetActive(true);
		}

		public void ShowFailMessage(JobRequestError failReason)
		{
			waitForSpawnTimer = 0;
			ShowJobSelection();

			errorReasonText.text = GetFailMessage(failReason);
			errorInfoWindow.SetActive(true);
		}

		private string GetFailMessage(JobRequestError failReason)
		{
			switch (failReason)
			{
				case JobRequestError.InvalidUserID:
					return "Invalid User ID (server issue).";
				case JobRequestError.InvalidPlayerID:
					return "Invalid Player ID.";
				case JobRequestError.RoundNotReady:
					return "New shift hasn't started yet.";
				case JobRequestError.JobBanned:
					return "You were previously fired from this position. [Job-banned]";
				case JobRequestError.PositionsFilled:
					return "All positions for this profession have been filled.";
				case JobRequestError.InvalidScript:
					return "Invalid ViewerScript (server issue).";
				default: return "Unspecified server error.";
			}
		}

		private void OnEnable()
		{
			screen_Jobs.SetActive(true);
			SetFooter();
			footer.SetActive(true);
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			expirementalJobsTestToggle.isOn = false;
			footer.DestroyAllChildren();
		}

		/// <summary>
		/// If a role has been selected this waits for the player to spawn.
		/// </summary>
		private void UpdateMe()
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
					ShowJobSelection();
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

			if (expirementalJobsTestToggle.isOn)
			{
				foreach (var jobAttribute in attributesJoinList.AttributesToUse)
				{
					ExpirementalSetupJobButton(jobAttribute);
				}
				screen_Jobs.SetActive(true);
				return;
			}

			foreach (Occupation occupation in OccupationList.Instance.Occupations)
			{
				SetupJobButton(occupation);
			}

			screen_Jobs.SetActive(true);
		}

		private void ExpirementalSetupJobButton(KeyValuePair<int, CharacterAttribute> jobAttribute)
		{
			GameObject occupationGO = Instantiate(buttonPrefab, screen_Jobs.transform);

			var image = occupationGO.GetComponent<Image>();
			var text = occupationGO.GetComponentInChildren<TextMeshProUGUI>();
			image.color = jobAttribute.Value.AttributeColorPallet.Count != 0 ?
				jobAttribute.Value.AttributeColorPallet[0] : Color.white;
			text.text = jobAttribute.Value.DisplayName;
			occupationGO.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
			occupationGO.GetComponent<Button>().onClick.AddListener(() => { BtnOk(jobAttribute); });
		}


		private void SetupJobButton(Occupation occupation)
		{
			JobType jobType = occupation.JobType;

			int active = GameManager.Instance.ClientGetOccupationsCount(jobType);
			int available = GameManager.Instance.GetOccupationMaxCount(jobType);

			GameObject occupationGO = Instantiate(buttonPrefab, screen_Jobs.transform);

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
				var entryTime = DateTime.ParseExact(check.dateTimeOfBan, "O", CultureInfo.InvariantCulture);
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

		/// <summary>
		/// Code for loading the footer, currently only containing a spectate button
		/// </summary>
		public void SetFooter()
		{
			var occupationGo = Instantiate(buttonPrefab, footer.transform);
			occupationGo.GetComponent<Image>().color = Color.white;
			occupationGo.GetComponentInChildren<TextMeshProUGUI>().text = "Spectate";
			occupationGo.GetComponent<Button>().onClick.AddListener(() => { PlayerManager.LocalViewerScript.Spectate(); });

			var occupationRandom = Instantiate(buttonPrefab, footer.transform);
			occupationRandom.GetComponent<Image>().color = Color.gray;
			occupationRandom.GetComponentInChildren<TextMeshProUGUI>().text = "Random";
			occupationRandom.GetComponent<Button>().onClick.AddListener(RandomJob);
		}

		public void ToggleExpierementalStuff()
		{
			UpdateJobsList();
			jobInfo.gameObject.SetActive(expirementalJobsTestToggle.isOn == false);
			expiermentalWarning.SetActive(expirementalJobsTestToggle.isOn);
		}

		private void RandomJob()
		{
			var possibleJobs = screen_Jobs.transform.GetComponentsInChildren<Button>().FindAll(x => x.interactable);
			if (possibleJobs.Length == 0)
			{
				ModalPanelManager.Instance.Inform("No jobs available.");
				return;
			}
			possibleJobs.PickRandom().onClick.Invoke();
		}
	}


}
