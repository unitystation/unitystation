using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// tg style action tooltip
/// </summary>
public class ActionTooltip : MonoBehaviour
{
	[SerializeField] private Text nameText;
	[SerializeField] private Text descriptionText;

	public void ApplyActionData(ActionData actionData)
	{
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
