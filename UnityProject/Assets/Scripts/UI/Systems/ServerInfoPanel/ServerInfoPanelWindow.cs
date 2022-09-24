using System.Collections.Generic;
using UnityEngine;

namespace UI.Systems.ServerInfoPanel
{
	public class ServerInfoPanelWindow: MonoBehaviour
	{
		[field: SerializeField]
		public MotdPage MotdPage { get; private set; }

		[field: SerializeField]
		public RulesPage RulesPage { get; private set; }

		[field: SerializeField]
		public EventsPage EventsPage { get; private set; }

		[field: SerializeField]
		public ChangelogPage ChangelogPage { get; private set; }

		[SerializeField] private GameObject motdButton;
		[SerializeField] private GameObject rulesButton;
		[SerializeField] private GameObject eventsButton;
		[SerializeField] private GameObject changelogButton;

		private readonly Dictionary<GameObject, InfoPanelPage> buttonPanelRelations = new();

		private void Awake()
		{
			buttonPanelRelations.Add(motdButton, MotdPage);
			buttonPanelRelations.Add(rulesButton, RulesPage);
			buttonPanelRelations.Add(eventsButton, EventsPage);
			buttonPanelRelations.Add(changelogButton, ChangelogPage);
		}

		private void OnEnable()
		{

			RefreshWindow();
		}

		public void RefreshWindow()
		{
			gameObject.SetActive(true);
			HideAllButtons();
			ShowRelevantButtons();
			ShowFirstPage();
		}

		private void HideAllPages()
		{
			MotdPage.gameObject.SetActive(false);
			RulesPage.gameObject.SetActive(false);
			EventsPage.gameObject.SetActive(false);
			ChangelogPage.gameObject.SetActive(false);
		}

		private void HideAllButtons()
		{
			motdButton.SetActive(false);
			rulesButton.SetActive(false);
			eventsButton.SetActive(false);
			changelogButton.SetActive(false);
		}

		private void ShowRelevantButtons()
		{
			foreach (var kvp in buttonPanelRelations)
			{
				if (kvp.Value != null && kvp.Value.HasContent())
				{
					kvp.Key.SetActive(true);
				}
			}
		}

		private void ShowFirstPage()
		{
			HideAllPages();
			foreach (var kvp in buttonPanelRelations)
			{
				if (kvp.Value == null || kvp.Value.HasContent() == false) continue;
				kvp.Value.gameObject.SetActive(true);
				return;
			}
		}

		public void OnMotdPageButtonClicked()
		{
			HideAllPages();
			MotdPage.gameObject.SetActive(true);
		}

		public void OnRulesPageButtonClicked()
		{
			HideAllPages();
			RulesPage.gameObject.SetActive(true);
		}

		public void OnEventsPageButtonClicked()
		{
			HideAllPages();
			EventsPage.gameObject.SetActive(true);
		}

		public void OnChangelogPageButtonClicked()
		{
			HideAllPages();
			ChangelogPage.gameObject.SetActive(true);
		}
	}
}