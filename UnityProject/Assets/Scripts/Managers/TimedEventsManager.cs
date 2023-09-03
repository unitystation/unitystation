using System;
using System.Collections.Generic;
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
	public class TimedEventsManager : SingletonManager<TimedEventsManager>, IInitialise
	{
		[SerializeField] private List<TimedGameEventSO> events;
		private List<TimedGameEventSO> activeEvents = new List<TimedGameEventSO>();

		public List<TimedGameEventSO> ActiveEvents => activeEvents;

		public InitialisationSystems Subsystem { get; }

		public void Initialise()
		{
			Loggy.Log("[Subsystems/TimedEvents] - Setting up event hooks.");
			EventManager.AddHandler(Event.RoundStarted, StartActiveEvents);
			EventManager.AddHandler(Event.ScenesLoadedServer, CleanAndUpdateActiveEvents);
			EventManager.AddHandler(Event.RoundEnded, EndActiveEvents);
		}

		public override void Awake()
		{
			base.Awake();
			//Update on awake so the UI can see what events are there.
			UpdateActiveEvents();
		}

		private void CleanAndUpdateActiveEvents()
		{
			Loggy.Log("[SubSystems/TimedEvents] - Cleaning active events.");
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
			activeEvents.Clear();
			foreach (TimedGameEventSO eventSo in events)
			{
				if ((int)eventSo.Month != DateTime.Now.Month) continue;
				if (DateTime.Today.Day.IsBetween(eventSo.DayOfMonthStart, eventSo.DayOfMonthEnd) == false) continue;
				eventSo.Clean();
				activeEvents.Add(eventSo);
			}
		}
	}
}

