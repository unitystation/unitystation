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

	/// <summary>
	/// Sets the job entry text and sprite
	/// </summary>
	/// <param name="name">The display name of the job</param>
	/// <param name="sprite">The sprite to show for the job</param>
	public void Set(string name, Sprite sprite)
	{
		jobName.text = name;
		jobImage.sprite = sprite;
	}
}
