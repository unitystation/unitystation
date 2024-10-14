using System;
using System.Collections.Generic;
using System.Linq;
using Initialisation;
using Logs;
using UnityEngine;
using ScriptableObjects.TimedGameEvents;
using Shared.Managers;

namespace Managers
{
	/// <summary>
	/// Manager that handles timed game events that only happen under a specific time of the year/month/week/day
	/// </summary>
	/// TODO : Allow admins to create and save events for their sever
	public class TimedEventsManager : SingletonManager<TimedEventsManager>
	{
		[SerializeField] private List<TimedGameEventSO> events;

		private List<TimedGameEventSO> activeEvents = new List<TimedGameEventSO>();
		public List<TimedGameEventSO> ActiveEvents => activeEvents;

		public override void Awake()
		{
			base.Awake();
			//Update on awake so the UI can see what events are there.
			Loggy.Log("[Subsystems/TimedEvents] - Setting up event hooks.");
			EventManager.AddHandler(Event.RoundStarted, StartActiveEvents);
			EventManager.AddHandler(Event.ScenesLoadedServer, CleanAndUpdateActiveEvents);
			EventManager.AddHandler(Event.RoundEnded, EndActiveEvents);
			UpdateActiveEvents();
		}

		private void OnDisable()
		{
			EndActiveEvents();
			activeEvents.Clear();
		}

		public override void OnDestroy()
		{
			EndActiveEvents();
			activeEvents.Clear();
			base.OnDestroy();
		}

		private void CleanAndUpdateActiveEvents()
		{
			Loggy.Log("[SubSystems/TimedEvents] - Cleaning active events.");
			activeEvents.Clear();
			UpdateActiveEvents();
		}

		private void StartActiveEvents()
		{
			Loggy.Log("[Subsystems/TimedEvents] - Starting timed events.");
			foreach (var timedEvent in activeEvents)
			{
				StartCoroutine(timedEvent.EventStart());
			}
		}

		private void EndActiveEvents()
		{
			Loggy.Log("[Subsystems/TimedEvents] - Stopping timed events.");
			foreach (var timedEvent in activeEvents)
			{
				StartCoroutine(timedEvent.OnRoundEnd());
				timedEvent.Clean();
			}
		}

		private void UpdateActiveEvents()
		{
			foreach (TimedGameEventSO eventSo in events)
			{
				if (eventSo.Months.Any(month =>
					    (int)month == DateTime.Now.Month &&
					    eventSo.MonthDayRanges.TryGetValue(month, out var dayRange) &&
					    DateTime.Today.Day.IsBetween(dayRange.DayOfMonthStart, dayRange.DayOfMonthEnd)))
				{
					eventSo.Clean();
					activeEvents.Add(eventSo);
				}
				else
				{
					Loggy.Log("[Subsystems/TimedEvents] - Event not active. "
					          + eventSo.EventName + " is not active on " + DateTime.Now.ToString("MMMM"), Category.Event);
				}
			}
		}
	}
}

