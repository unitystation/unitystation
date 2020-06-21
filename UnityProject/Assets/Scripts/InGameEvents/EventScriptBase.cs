using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGameEvents
{
	public class EventScriptBase : MonoBehaviour
	{
		/// <summary>
		/// Delayed method start time
		/// </summary>
		public float StartTimer = 0f;
		/// <summary>
		/// Delayed method end time
		/// </summary>
		public float EndTimer = 0f;

		/// <summary>
		/// Event Type, for categories
		/// </summary>
		public InGameEventType EventType = InGameEventType.Fun;

		/// <summary>
		/// Chance to happen 1-100%
		/// </summary>
		public int ChanceToHappen = 100;

		public virtual void OnEventStart()
		{
			Invoke(nameof(OnEventStartTimed), StartTimer);
		}

		public virtual void OnEventStartTimed()
		{
			OnEventEnd();
		}

		public virtual void OnEventEnd()
		{
			Invoke(nameof(OnEventEndTimed), StartTimer);
		}

		public virtual void OnEventEndTimed()
		{

		}

		public void TriggerEvent()
		{
			OnEventStart();
		}
	}

	public enum InGameEventType
	{
		Fun,
		Debug
	}
}