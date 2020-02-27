using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The job entry script which allows setting the entry text and image.
/// </summary>
public class JobListEntry : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The job TMP label component")]
	private TMP_Text jobName;
	[SerializeField]
	[Tooltip("The job image component")]
	private Image jobImage;

	[SerializeField]
	[Tooltip("The priority dropdown component for this job")]
	private TMP_Dropdown dropdown;

	[SerializeField]
	private GUI_JobPreferences jobPreferences;

	private JobType jobType;

	/// <summary>
	/// Sets all fields for this entry from an occupation
	/// </summary>
	/// <param name="occupation"></param>
	public void Setup(Occupation occupation)
	{
		jobType = occupation.JobType;
		jobName.text = occupation.DisplayName;
		jobImage.sprite = occupation.PreviewSprite;
	}

	/// <summary>
	/// Sets the priority for this job entry
	/// </summary>
	/// <param name="priority"></param>
	public void SetPriority(Priority priority)
	{
		dropdown.value = (int)priority;
	}

	/// <summary>
	/// Sends the priority update to the main job preferences script for processing
	/// </summary>
	/// <param name="value">The updated priority value as an int</param>
	public void OnValueChanged(int value)
	{
		Priority priority = (Priority)value;
		jobPreferences.OnPriorityChange(jobType, priority, this);
	}
}
