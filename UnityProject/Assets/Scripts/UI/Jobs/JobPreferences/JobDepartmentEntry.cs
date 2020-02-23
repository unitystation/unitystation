using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JobDepartmentEntry : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The header text for this department")]
	private TMP_Text header;

	[SerializeField]
	[Tooltip("The background image for this department")]
	private Image headerBackground;

	[SerializeField]
	[Tooltip("The template to use for each job")]
	private JobListEntry jobEntryTemplate;

	/// <summary>
	/// Adds a job entry for an occupation
	/// </summary>
	/// <param name="occupation">The occupation to add</param>
	public void Add(Occupation occupation)
	{
		GameObject newEntry = Instantiate(jobEntryTemplate.gameObject, jobEntryTemplate.transform.parent);
		var jobEntry = newEntry.GetComponent<JobListEntry>();
		jobEntry.Set(occupation.DisplayName, occupation.PreviewSprite);
		newEntry.SetActive(true);
	}

	/// <summary>
	/// Adds job entries for a collection of occupations
	/// </summary>
	/// <param name="occupations">The collection occupations to add</param>
	public void Add(IEnumerable<Occupation> occupations)
	{
		foreach (var occupation in occupations)
		{
			Add(occupation);
		}
	}

	/// <summary>
	/// Sets the department header text and color, and creates job entries
	/// for each occupation in the department
	/// </summary>
	/// <param name="department"></param>
	public void Set(Department department)
	{
		Logger.Log($"SETTING DEPARTMENT: {department.DisplayName}");
		header.text = department.DisplayName;
		headerBackground.color = department.HeaderColor;
		Add(department.Occupations);
	}
}
