using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A window for displaying information about a certain job.
/// The window contains a preview image and other useful info.
/// </summary>
public class GUI_JobInfo : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The job info to display on this job info panel.")]
	private Occupation job = null;
	public Occupation Job
	{
		get => job;
		set
		{
			if (job != value)
			{
				job = value;
				RefreshVisuals();
			}
		}
	}

	[Header("References")]

	[SerializeField]
	[Tooltip("The job title.")]
	private Text title = null; // We might want to replace this with TMP, but it would be inconsistent with the job select screen.

	[SerializeField]
	[Tooltip("Contains the summary of the job.")]
	private TMPro.TextMeshProUGUI summary = null;

	[SerializeField]
	[Tooltip("A multi-paragraph description of the job for players new to the role.")]
	private TMPro.TextMeshProUGUI description = null;

	[SerializeField]
	[Tooltip("Shows the player what the uniform looks like.")]
	private Image previewImage = null;

	[Header("Colors")]

	[SerializeField]
	[Tooltip("Progress will be colored red until progress reaches this value.")]
	private int colorThresholdProgressRed = 20;

	[SerializeField]
	[Tooltip("Progress will be colored red until progress reaches this value.")]
	private int colorThresholdProgressYellow = 66;
	// Values that are higher will be green.

	/// <summary>
	/// Make sure the correct job is shown when the script is loaded (in the editor, too).
	/// </summary>
	private void OnValidate()
	{
		RefreshVisuals();
	}

	/// <summary>
	/// Refreshes the visuals of the job info panel
	/// </summary>
	private void RefreshVisuals()
	{
		// Title
		if (title != null)
		{
			title.text = job.DisplayName;
			title.color = job.ChoiceColor;
		}
		
		// Preview image
		if (previewImage != null)
		{
			previewImage.sprite = job.PreviewSprite;
		}

		// Summary (Difficulty, development progress, duties bulletpoints)
		if (summary != null)
		{
			summary.text =
				GetFormattedDifficulty(job.Difficulty) + "\n" +
				GetFormattedProgress(job.Progress) + "\n" +
				job.DescriptionShort;
		}

		// Description
		if (description != null)
		{
			if (String.IsNullOrEmpty(job.DescriptionLong))
			{
				description.text = "<i>Description coming soon!<i>";
			}
			else
			{
				description.text = job.DescriptionLong;
			}
		}
	}

	/// <summary>
	/// Returns a colored and formatted string of the implementation progress.
	/// </summary>
	/// <param name="progress">How much of the role has been implemented.</param>
	/// <returns>The progress with color style tags and other formatting.</returns>
	private string GetFormattedProgress(int progress)
	{
		string result = "<b>Progress: ";

		switch (progress) {
			case int n when (n < colorThresholdProgressRed):
				result += "<color=red>";
				break;
			case int n when (n < colorThresholdProgressYellow):
				result += "<color=yellow>";
				break;
			default:
				result += "<color=green>";
				break;
		}

		result += $"{progress}%</color></b>";

		return result;
	}

	/// <summary>
	/// Returns a string for the occupation's difficulty.
	/// Has a nice prefix and color tags, too.
	/// </summary>
	/// <param name="difficulty">The difficulty of the job</param>
	/// <returns>The difficulty as a string with color tags and other formatting.</returns>
	private string GetFormattedDifficulty(Occupation.OccupationDifficulty difficulty)
	{
		string result = "<b>Difficulty: ";

		switch (difficulty)
		{
			case Occupation.OccupationDifficulty.Zero:
				result += "<color=green>";
				break;
			case Occupation.OccupationDifficulty.Low:
				result += "<color=green>";
				break;
			case Occupation.OccupationDifficulty.Medium:
				result += "<color=yellow>";
				break;
			case Occupation.OccupationDifficulty.High:
				result += "<color=red>";
				break;
			case Occupation.OccupationDifficulty.Extreme:
				result += "<color=red>";
				break;
		}

		result += $"{difficulty}</color></b>";

		return result;
	}
}
