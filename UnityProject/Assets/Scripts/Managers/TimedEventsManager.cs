using System;
using System.Collections.Generic;
using Initialisation;
using UnityEngine;
using ScriptableObjects.TimedGameEvents;
using Shared.Managers;

namespace Managers
{
	/// <summary>
	/// Manager that handles timed game events that only happen under a specific time of the year/month/week/day
	/// </summary>
	/// TODO : Allow admins to create and save events for their sever
	public class TimedEventsManager : SingletonManager<TimedEventsManager>, Initialisation.IInitialise
	{
		[SerializeField] private List<TimedGameEventSO> events;
		private List<TimedGameEventSO> activeEvents = new List<TimedGameEventSO>();

		public List<TimedGameEventSO> ActiveEvents => activeEvents;

		public InitialisationSystems Subsystem { get; }

		public void Initialise()
		{
			EventManager.AddHandler(Event.RoundStarted, StartActiveEvents);
			EventManager.AddHandler(Event.RoundEnded, EndActiveEvents);
			Debug.Log("initalising event hooks");
		}

		public override void Awake()
		{
			base.Awake();
			UpdateActiveEvents();
		}

		private void StartActiveEvents()
		{
			foreach (var timedEvent in activeEvents)
			{
				StartCoroutine(timedEvent.EventStart());
			}
		}

		private void EndActiveEvents()
		{
			foreach (var timedEvent in activeEvents)
			{
				StartCoroutine(timedEvent.OnRoundEnd());
			}
			EventManager.RemoveHandler(Event.RoundStarted, StartActiveEvents);
			EventManager.RemoveHandler(Event.RoundEnded, EndActiveEvents);
		}

		private void UpdateActiveEvents()
		{
			foreach (TimedGameEventSO eventSo in events)
			{
				if ((int)eventSo.Month != DateTime.Now.Month) continue;
				if (DateTime.Today.Day.IsBetween(eventSo.DayOfMonthStart, eventSo.DayOfMonthEnd) == false) continue;
				activeEvents.Add(eventSo);
			}
		}
	}
}

