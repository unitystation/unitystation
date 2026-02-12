using System;
using Logs;
using TMPro;
using UnityEngine;
using US13.Actions.V2;
using Util.Independent.FluentRichText;

namespace US13.Actions
{
	/// <summary>
	/// tg style action tooltip
	/// </summary>
	public class ActionTooltip : MonoBehaviour
	{
		[SerializeField] private TMP_Text nameText = default;
		[SerializeField] private TMP_Text descriptionText = default;

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
				descriptionText.text = actionData.Description + "\n\n [OBSOLETE] - This button is spawned from an outdated system.\n Please port this to the newer V2 System.".Color(Color.red);
			}
		}

		public void ApplyActionData(ActionButtonData actionData)
		{
			if (actionData == null)
			{
				Loggy.Error("action data is missing when attempting to apply tooltip data.");
				return;
			}
			if (string.IsNullOrEmpty(actionData.DisplayName))
			{
				nameText.text = String.Empty;
				nameText.enabled = false;
			}
			else
			{
				nameText.enabled = true;
				nameText.text = actionData.DisplayName;
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
}
