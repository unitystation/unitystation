using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Events
{
	public class UIEvent : UnityEvent<GameObject>
	{
	}

	//For simple broadcasts:
	public enum EVENT
	{
		UpdateFov
	} // + other events. Add them as you need them

	[ExecuteInEditMode]
	public class EventManager : MonoBehaviour
	{
		// Stores the delegates that get called when an event is fired (Simple Events)
		private static readonly Dictionary<EVENT, Action> eventTable
			= new Dictionary<EVENT, Action>();

		private static EventManager eventManager;
		private readonly EventController<string, GameObject> ui = new EventController<string, GameObject>();

		public static EventController<string, GameObject> UI => Instance.ui;

		public static EventManager Instance
		{
			get
			{
				if (!eventManager)
				{
					eventManager = FindObjectOfType<EventManager>();
				}
				return eventManager;
			}
		}

		public static void UpdateLights()
		{
		}

		/*
		   * Below is for the simple event handlers and broast methods:
		   */

		// Adds a delegate to get called for a specific event
		public static void AddHandler(EVENT evnt, Action action)
		{
			if (!eventTable.ContainsKey(evnt))
			{
				eventTable[evnt] = action;
			}
			else
			{
				eventTable[evnt] += action;
			}
		}

		public static void RemoveHandler(EVENT evnt, Action action)
		{
			if (eventTable[evnt] != null)
			{
				eventTable[evnt] -= action;
			}
			if (eventTable[evnt] == null)
			{
				eventTable.Remove(evnt);
			}
		}

		// Fires the event
		public static void Broadcast(EVENT evnt)
		{
			if (eventTable.ContainsKey(evnt) && eventTable[evnt] != null)
			{
				eventTable[evnt]();
			}
		}
	}
}