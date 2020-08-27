using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class UIEvent : UnityEvent<GameObject>
{
}

//For simple broadcasts:
public enum EVENT
{
	UpdateFov,
	PowerNetSelfCheck,
	ChatFocused,
	ChatUnfocused,
	LoggedOut,
	RoundStarted,
	RoundEnded,
	DisableInternals,
	EnableInternals,
	PlayerSpawned,
	PlayerDied,
	GhostSpawned,
	LogLevelAdjusted,
	UpdateChatChannels,
	ToggleChatBubbles,
	PlayerRejoined,
	PreRoundStarted,
	MatrixManagerInit
} // + other events. Add them as you need them

[ExecuteInEditMode]
public class EventManager : MonoBehaviour
{
	// Stores the delegates that get called when an event is fired (Simple Events)
	private static readonly Dictionary<EVENT, Action> eventTable
		= new Dictionary<EVENT, Action>();

	private static EventManager eventManager;

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
		if (!eventTable.ContainsKey(evnt)) return;
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
		LogEventBroadcast(evnt);
		if (eventTable.ContainsKey(evnt) && eventTable[evnt] != null)
		{
			eventTable[evnt]();

		}
	}

	/// <summary>
	/// Calls the appropriate logging category for the event
	/// </summary>
	/// <param name="evnt"></param>
	private static void LogEventBroadcast(EVENT evnt)
	{
		string msg = "Broadcasting a " + evnt + " event";
		Category category;

		switch (evnt)
		{
			case EVENT.ChatFocused:
			case EVENT.ChatUnfocused:
			case EVENT.UpdateChatChannels:
			case EVENT.UpdateFov:
				category = Category.UI;
				break;
			case EVENT.DisableInternals:
			case EVENT.EnableInternals:
				category = Category.Equipment;
				break;
			case EVENT.LoggedOut:
				category = Category.Connections;
				break;
			case EVENT.PlayerDied:
				category = Category.Health;
				break;
			case EVENT.PowerNetSelfCheck:
				category = Category.Electrical;
				break;
			case EVENT.RoundStarted:
			case EVENT.RoundEnded:
				category = Category.Round;
				break;
			default:
				category = Category.Unknown;
				break;
		}


		Logger.LogTrace(msg, category);


	}
}
