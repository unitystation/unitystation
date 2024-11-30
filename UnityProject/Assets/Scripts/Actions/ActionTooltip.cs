using System;
using Logs;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// tg style action tooltip
/// </summary>
public class ActionTooltip : MonoBehaviour
{
	[SerializeField] private Text nameText = default;
	[SerializeField] private Text descriptionText = default;

	public void ApplyActionData(ActionData actionData)
	{
		if (actionData == null)
		{
			Loggy.Error("action data is missing when attempting to apply tooltip data.");
			return;
		}
		if (string.IsNullOrEmpty(actionData.Name))
		{
			nameText.text = String.Empty;
			nameText.enabled = false;
		}
		else
		{
			nameText.enabled = true;
			nameText.text = actionData.Name;
		}

		if (string.IsNullOrEmpty(actionData.Description))
		{
			descriptionText.text = String.Empty;
			descriptionText.enabled = false;
		}
		else
		{
			descriptionText.enabled = true;
			descriptionText.text = actionData.Description;
		}
	}
}
