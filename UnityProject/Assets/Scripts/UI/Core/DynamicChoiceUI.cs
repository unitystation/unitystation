using System;
using System.Collections.Generic;
using IngameDebugConsole;
using Shared.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core
{
	/// <summary>
	/// Client side menu that lists a bunch of choices that invokes commands.
	/// </summary>
	public class DynamicChoiceUI : SingletonManager<DynamicChoiceUI>
	{
		[SerializeField] private Transform entryPrefab;
		[SerializeField] private Transform scrollContent;
		[SerializeField] private Transform closeButton;
		[SerializeField] private TMP_Text windowText;
		[SerializeField] private TMP_Text choiceInfoText;

		public System.Action OnChoiceTaken;

		public override void Awake()
		{
			base.Awake();
			OnChoiceTaken += HideUI;
			this.SetActive(false);
		}

		public override void OnDestroy()
		{
			OnChoiceTaken -= HideUI;
			base.OnDestroy();
		}

		public static void ClientDisplayChoicesNotNetworked(string windowName, string choiceInfo, List<DynamicUIChoiceEntryData> choices, bool allowClose = false)
		{
			Instance.SetActive(true);
			Instance.ClearChoices();
			foreach (var choice in choices)
			{
				var entry = Instantiate(Instance.entryPrefab, Instance.scrollContent);
				entry.GetComponent<DynamicChoiceEntry>().Setup(choice.Text, choice.Icon, choice.ChoiceAction);
				entry.SetActive(true);
			}
			Instance.closeButton.SetActive(allowClose);
			Instance.windowText.text = windowName;
			Instance.choiceInfoText.text = choiceInfo;
		}

		private void ClearChoices()
		{
			int childCount = scrollContent.childCount;
			for (int i = childCount - 1; i >= 0; i--)
			{
				Transform childTransform = scrollContent.GetChild(i);
				Destroy(childTransform.gameObject);
			}
		}

		private void HideUI()
		{
			this.SetActive(false);
		}
	}

	public struct DynamicUIChoiceEntryData
	{
		public string Text;
		public Sprite Icon;
		public System.Action ChoiceAction;
	}
}