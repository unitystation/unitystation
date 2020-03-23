using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Job priority enum used for setting job preferences
/// </summary>
public enum Priority
{
	None = 0,
	Low = 1,
	Medium = 2,
	High = 3
}

/// <summary>
/// Controls the job preferences screen
/// </summary>
public class GUI_JobPreferences : MonoBehaviour
{
	/// <summary>
	/// Have all the jobs already been populated?
	/// </summary>
	private bool jobsPopulated;

	[SerializeField]
	[Tooltip("The job department template component")]
	private JobDepartmentEntry departmentEntryTemplate;

	[SerializeField]
	[Tooltip("The grid which contains all of the departments")]
	private GameObject departmentGrid;

	[SerializeField]
	[Tooltip("Image to hide the grid while it resizes")]
	private GameObject gridShroud;

	/// <summary>
	/// A reference to each department entry so they can be destroyed later.
	/// </summary>
	private Dictionary<Department, GameObject> departmentEntries = new Dictionary<Department, GameObject>();

	/// <summary>
	/// References to each JobListEntry by JobType which areused to
	/// change the dropdown values.
	/// </summary>
	private Dictionary<JobType, JobListEntry> jobEntries = new Dictionary<JobType, JobListEntry>();

	/// <summary>
	/// Stores the job with the 'High' priority.
	/// Used to ensure only one job has 'High' priority.
	/// </summary>
	// private JobType highPreference;

	private JobListEntry highEntry;

	/// <summary>
	/// Stores all of the player's job preferences, with job type and priority.
	/// </summary>
	private Dictionary<JobType, Priority> jobPreferences = new Dictionary<JobType, Priority>();

	private void OnEnable()
	{
		PopulateJobs();
	}

	/// <summary>
	/// Populates all the departments and occupations in the job preferences window
	/// </summary>
	private void PopulateJobs(bool force = false)
	{
		if (!force && jobsPopulated)
		{
			Logger.Log("Jobs have already been populated!", Category.UI);
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
		Logger.Log($"Changed priority for {job} to {priority}.", Category.UI);

		if (priority == Priority.None)
		{
			// Only include jobs with a priority
			jobPreferences.Remove(job);
		}
		else
		{
			if (priority == Priority.High)
			{
				// Downgrade the previous High priority job to Medium
				highEntry?.SetPriority(Priority.Medium);
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

		Logger.Log("Current Job Preferences:\n" +
			string.Join("\n", jobPreferences.Select(a => $"{a.Key}: {a.Value}")), Category.UI);
	}
}
