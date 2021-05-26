using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
	public class JobDepartmentEntry : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("The header text for this department")]
		private TMP_Text header = null;

		[SerializeField]
		[Tooltip("The background image for this department")]
		private Image headerBackground = null;

		[SerializeField]
		[Tooltip("The template to use for each job")]
		private JobListEntry jobEntryTemplate = null;

		/// <summary>
		/// Adds a job entry for an occupation
		/// </summary>
		/// <param name="occupation">The occupation to add</param>
		public void Add(Occupation occupation, ref Dictionary<JobType, JobListEntry> jobEntries)
		{
			GameObject newEntry = Instantiate(jobEntryTemplate.gameObject, jobEntryTemplate.transform.parent);
			var jobEntry = newEntry.GetComponent<JobListEntry>();
			// Add a reference so the entry can be manipulated later
			jobEntries.Add(occupation.JobType, jobEntry);
			jobEntry.Setup(occupation);
			newEntry.SetActive(true);
		}

		/// <summary>
		/// Adds job entries for a collection of occupations
		/// </summary>
		/// <param name="occupations">The collection occupations to add</param>
		public void Add(IEnumerable<Occupation> occupations, ref Dictionary<JobType, JobListEntry> jobEntries)
		{
			foreach (var occupation in occupations)
			{
				Add(occupation, ref jobEntries);
			}
		}

		/// <summary>
		/// Sets the department header text and color, and creates job entries
		/// for each occupation in the department
		/// </summary>
		/// <param name="department"></param>
		public void Setup(Department department, ref Dictionary<JobType, JobListEntry> jobEntries)
		{
			header.text = department.DisplayName;
			headerBackground.color = department.HeaderColor;
			Add(department.AllOccupations, ref jobEntries);
		}
	}
}
