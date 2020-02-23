using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Job priority enum
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

	private void OnEnable()
	{
		PopulateJobs();
	}

	/// <summary>
	/// Populates all the departments and occupations in the job preferences window
	/// </summary>
	private void PopulateJobs()
	{
		if (jobsPopulated)
		{
			Logger.Log("Jobs have already been populated!", Category.UI);
			return;
		}

		gridShroud.SetActive(true);

		// Add and populate each department
		foreach (var department in DepartmentList.Instance.Departments)
		{
			GameObject newEntry = Instantiate(departmentEntryTemplate.gameObject, departmentEntryTemplate.transform.parent);
			var departmentEntry = newEntry.GetComponent<JobDepartmentEntry>();
			departmentEntry.Set(department);
			newEntry.SetActive(true);
		}
		jobsPopulated = true;

		// Resize the grid to let the FlowLayoutGroup arrange everything correctly
		StartCoroutine(ResizeGrid());
	}

	IEnumerator ResizeGrid()
	{
		yield return WaitFor.EndOfFrame;
		departmentGrid.SetActive(false);
		departmentGrid.SetActive(true);
		gridShroud.SetActive(false);
	}
}
