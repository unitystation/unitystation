using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Shared.Managers;
using UnityEngine;
using US13.ScriptableObjects.TimedGameEvents;
using Util;

namespace US13.Managers
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
			Loggy.Info("Setting up event hooks.", Category.Event);
			EventManager.AddHandler(Event.RoundStarted, StartActiveEvents);
			EventManager.AddHandler(Event.ScenesLoadedServer, CleanAndUpdateActiveEvents);
			EventManager.AddHandler(Event.RoundEnded, EndActiveEventsPrebool);
			UpdateActiveEvents();
		}

		private void OnDisable()
		{
			EndActiveEvents();
			activeEvents.Clear();
		}

		public override void OnDestroy()
		{
			EndActiveEvents(true);
			activeEvents.Clear();
			EventManager.RemoveHandler(Event.RoundStarted, StartActiveEvents);
			EventManager.RemoveHandler(Event.ScenesLoadedServer, CleanAndUpdateActiveEvents);
			EventManager.RemoveHandler(Event.RoundEnded, EndActiveEventsPrebool);
			base.OnDestroy();
		}

		private void CleanAndUpdateActiveEvents()
		{
			Loggy.Info("Cleaning active events.", Category.Event);
			activeEvents.Clear();
			UpdateActiveEvents();
		}

		private void StartActiveEvents()
		{
			Loggy.Info("Starting timed events.", Category.Event);
			foreach (var timedEvent in activeEvents)
			{
				StartCoroutine(timedEvent.EventStart());
			}
		}

		private void EndActiveEventsPrebool()
		{
			EndActiveEvents();
		}

		private void EndActiveEvents(bool BeingDestroyed = false)
		{
			Loggy.Info("Stopping timed events.", Category.Event);
			foreach (var timedEvent in activeEvents)
			{
				if (BeingDestroyed == false)
				{
					StartCoroutine(timedEvent.OnRoundEnd());
				}

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
					Loggy.Info(
						$"Event not active. {eventSo.EventName} is not active on DateTime.Now.ToString(\"MMMM\")",
						Category.Event);
				}
			}
		}
	}
}

