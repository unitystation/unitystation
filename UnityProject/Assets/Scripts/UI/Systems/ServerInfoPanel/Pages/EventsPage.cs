using System.Collections.Generic;
using Managers;
using ScriptableObjects.TimedGameEvents;
using UI.Systems.PreRound;
using UnityEngine;

namespace UI.Systems.ServerInfoPanel
{
	public class EventsPage: InfoPanelPage
	{
		[SerializeField] private GameObject eventPrefab;
		[SerializeField] private GameObject eventsContainer;

		private List<TimedGameEventSO> events;

		private bool TryFindEvents()
		{
			//We fetched the list of events before, simply return if there are events
			if (events is {}) return events.Count > 0;

			//Fetch the list of events for the first time
			events = TimedEventsManager.Instance.ActiveEvents;

			if (events.Count > 0)
			{
				//There are events, let's populate the page
				PopulatePage();
			}

			return events.Count > 0;
		}

		private void PopulatePage()
		{
			foreach (var @event in events)
			{
				var newEvent = Instantiate(eventPrefab, eventsContainer.transform, true);
				var entry = newEvent.AddComponent<EventEntry>();
				entry.SetEvent(@event);
			}
		}

		public override bool HasContent()
		{
			return TryFindEvents();
		}
	}
}