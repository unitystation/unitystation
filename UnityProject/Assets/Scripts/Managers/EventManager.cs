using System;
using System.Collections.Generic;
using Messages.Server;
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
	PostRoundStarted,
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
	MatrixManagerInit,
	BlobSpawned
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

	/// <summary>
	/// Trigger the given event. If networked, will trigger the event on all clients.
	/// </summary>
	public static void Broadcast(EVENT evnt, bool network = false)
	{
		LogEventBroadcast(evnt);
		if (eventTable.ContainsKey(evnt) == false || eventTable[evnt] == null) return;

		if (CustomNetworkManager.IsServer && network)
		{
			TriggerEventMessage.SendToAll(evnt);
		}
		else
		{
			eventTable[evnt]();
		}
	}

	/// <summary>
	/// Calls the appropriate logging category for the event
	/// </summary>
	private static void LogEventBroadcast(EVENT evnt)
	{
		string msg = "Broadcasting a " + evnt + " event";
		Category category;

		switch (evnt)
		{
			case EVENT.ChatFocused:
				category = Category.Chat;
				break;
			case EVENT.ChatUnfocused:
				category = Category.Chat;
				break;
			case EVENT.UpdateChatChannels:
				category = Category.Chat;
				break;
			case EVENT.ToggleChatBubbles:
				category = Category.Chat;
				break;
			case EVENT.UpdateFov:
				category = Category.UI;
				break;
			case EVENT.DisableInternals:
				category = Category.PlayerInventory;
				break;
			case EVENT.EnableInternals:
				category = Category.PlayerInventory;
				break;
			case EVENT.LoggedOut:
				category = Category.Connections;
				break;
			case EVENT.PlayerRejoined:
				category = Category.Connections;
				break;
			case EVENT.PlayerSpawned:
				category = Category.EntitySpawn;
				break;
			case EVENT.PlayerDied:
				category = Category.Health;
				break;
			case EVENT.GhostSpawned:
				category = Category.Ghosts;
				break;
			case EVENT.PowerNetSelfCheck:
				category = Category.Electrical;
				break;
			case EVENT.PreRoundStarted:
				category = Category.Round;
				break;
			case EVENT.RoundStarted:
				category = Category.Round;
				break;
			case EVENT.PostRoundStarted:
				category = Category.Round;
				break;
			case EVENT.RoundEnded:
				category = Category.Round;
				break;
			case EVENT.BlobSpawned:
				category = Category.Blob;
				break;
			case EVENT.MatrixManagerInit:
				category = Category.Matrix;
				break;
			default:
				category = Category.Unknown;
				break;
		}


		Logger.LogTrace(msg, category);


	}
}
