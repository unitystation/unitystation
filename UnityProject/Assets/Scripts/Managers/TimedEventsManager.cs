using System;
using System.Collections.Generic;
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
			UpdateActiveEvents();
		}

		public void UpdateActiveEvents()
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

