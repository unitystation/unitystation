using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;

/// <summary>
/// A dictionary using JobType and Priority. Used to store a player's job preferences.
/// </summary>
public class JobPrefsDict : Dictionary<JobType, Priority> { }

namespace UI.Character
{
	/// <summary>
	/// Controls the job preferences screen
	/// </summary>
	public class GUI_JobPreferences : MonoBehaviour
	{
		[SerializeField]
		private CharacterSettings characterSettings;

		/// <summary>
		/// Have all the jobs already been populated?
		/// </summary>
		private bool jobsPopulated;

		[SerializeField]
		[Tooltip("The job department template component")]
		private JobDepartmentEntry departmentEntryTemplate = null;

		[SerializeField]
		[Tooltip("The grid which contains all of the departments")]
		private GameObject departmentGrid = null;

		[SerializeField]
		[Tooltip("Image to hide the grid while it resizes")]
		private GameObject gridShroud = null;

		/// <summary>
		/// A reference to each department entry so they can be destroyed later.
		/// </summary>
		private Dictionary<Department, GameObject> departmentEntries = new Dictionary<Department, GameObject>();

		/// <summary>
		/// References to each JobListEntry by JobType which are used to
		/// change the dropdown values.
		/// </summary>
		private Dictionary<JobType, JobListEntry> jobEntries = new Dictionary<JobType, JobListEntry>();

		/// <summary>
		/// Stores the job with the 'High' priority.
		/// Used to ensure only one job has 'High' priority.
		/// </summary>
		private JobListEntry highEntry;

		/// <summary>
		/// Stores all of the player's job preferences, with job type and priority.
		/// </summary>
		private JobPrefsDict jobPreferences = new JobPrefsDict();

		public JobPrefsDict JobPreferences => jobPreferences;

		private void OnEnable()
		{
			PopulateJobs();
			LoadJobPreferences();
		}

		private void OnDisable()
		{
			SaveJobPreferences();
		}

		/// <summary>
		/// Populates all the departments and occupations in the job preferences window
		/// </summary>
		private void PopulateJobs(bool force = false)
		{
			if (force == false && jobsPopulated)
			{
				Loggy.Log("Jobs have already been populated!", Category.Jobs);
				return;
			}

			if (force)
			{
				ClearEntries();
			}

			gridShroud.SetActive(true);

			// Add and populate each department
			foreach (var department in DepartmentList.Instance.Departments)
			{
				GameObject newEntry = Instantiate(departmentEntryTemplate.gameObject, departmentEntryTemplate.transform.parent);
				departmentEntries.Add(department, newEntry);
				var departmentEntry = newEntry.GetComponent<JobDepartmentEntry>();
				departmentEntry.Setup(department, ref jobEntries);
				newEntry.SetActive(true);
			}
			jobsPopulated = true;

			// Resize the grid to let the FlowLayoutGroup arrange everything correctly
			StartCoroutine(ResizeGrid());
		}

		/// <summary>
		/// Clears all populated department and job entries
		/// </summary>
		private void ClearEntries()
		{
			foreach (var entry in departmentEntries)
			{
				Destroy(entry.Value);
			}
			departmentEntries.Clear();
			jobEntries.Clear();
		}

		/// <summary>
		/// Sets all jobs to a specific priority
		/// /// </summary>
		/// <param name="priority">The priority all jobs will be set to</param>
		public void SetAllPriorities(Priority priority)
		{
			foreach (var entry in jobEntries.Values)
			{
				entry.SetPriority(priority);
			}

		}

		/// <summary>
		/// Resets all job priorities to 'None'
		/// </summary>
		public void ResetAllPriorities()
		{
			SetAllPriorities(Priority.None);
			highEntry = null;

		}

		/// <summary>
		/// Forces the department grid to resize
		/// </summary>
		/// <returns></returns>
		private IEnumerator ResizeGrid()
		{
			yield return WaitFor.EndOfFrame;
			departmentGrid.SetActive(false);
			departmentGrid.SetActive(true);
			gridShroud.SetActive(false);
		}

		/// <summary>
		/// Updates job preferences and ensures there is only one 'High' priority job at a time.
		/// </summary>
		/// <param name="job">The job</param>
		/// <param name="priority">The priority</param>
		/// <param name="entry">Entry reference to change dropdown boxes</param>
		public void OnPriorityChange(JobType job, Priority priority, JobListEntry entry)
		{
			Loggy.Log($"Changed priority for {job} to {priority}.", Category.Jobs);

			if (priority == Priority.None)
			{
				// Only include jobs with a priority
				jobPreferences.Remove(job);
			}
			else
			{
				if (priority == Priority.High)
				{
					if (highEntry != null)
					{
						// Downgrade the previous High priority job to Medium
						highEntry.SetPriority(Priority.Medium);
					}
					highEntry = entry;
				}

				// Update job prefs with new priority
				if (!jobPreferences.ContainsKey(job))
				{
					jobPreferences.Add(job, priority);
				}
				else
				{
					jobPreferences[job] = priority;
				}
			}

			Loggy.Log("Current Job Preferences:\n" +
				string.Join("\n", jobPreferences.Select(a => $"{a.Key}: {a.Value}")), Category.Jobs);
		}

		/// <summary>
		/// Saves the current job preferences to CurrentCharacterSettings and updates the character profile
		/// </summary>
		private void SaveJobPreferences()
		{
			characterSettings.EditedCharacter.JobPreferences = jobPreferences;
		}

		/// <summary>
		/// Loads the job preferences from the CurrentCharacterSettings and updates jobPreferences via OnPriorityChange.
		/// </summary>
		private void LoadJobPreferences()
		{
			// Loop through all jobs and set the dropdown to the specified priority.
			// This will update the local jobPreferences variable using OnPriorityChange.
			foreach (var jobPref in characterSettings.EditedCharacter.JobPreferences.ToList())
			{
				jobEntries[jobPref.Key].SetPriority(jobPref.Value);
			}
		}

		/// <summary>
		/// Use this to add the assistant job priority to player, used for when they have no other job selected
		/// </summary>
		public void SetAssistantDefault()
		{
			OnPriorityChange(JobType.ASSISTANT, Priority.Low, null);
		}
	}
}
