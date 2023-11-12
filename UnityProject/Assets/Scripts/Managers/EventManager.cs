using System;
using System.Collections.Generic;
using Logs;
using Messages.Server;
using Shared.Util;
using UnityEngine;
using UnityEngine.Events;
using Util;


public class UIEvent : UnityEvent<GameObject> { }

// For simple broadcasts:
public enum Event
{
	UpdateFov,
	PowerNetSelfCheck,
	ChatFocused,
	ChatUnfocused,
	AccountLoggedOut,
	ServerLoggedOut,
	RoundStarted,
	PostRoundStarted,
	SceneUnloading,
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
	BlobSpawned,
	ScenesLoadedServer,
	LavalandFirstEntered,
	ChatQuickUnfocus,
	Cleanup,
	CleanupEnd,
	ReadyToInitialiseMatrices
} // + other events. Add them as you need them

[ExecuteInEditMode]
public class EventManager : MonoBehaviour
{
	// Stores the delegates that get called when an event is fired (Simple Events)
	private static readonly Dictionary<Event, List<Action>> eventTable
		= new Dictionary<Event, List<Action>>();

	private static EventManager eventManager;

	public static EventManager Instance => FindUtils.LazyFindObject(ref eventManager);

	public static void UpdateLights() { }

	/*
		* Below is for the simple event handlers and broast methods:
		*/

	// Adds a delegate to get called for a specific event
	public static void AddHandler(Event evnt, Action action)
	{
		if (!eventTable.ContainsKey(evnt))
		{
			eventTable[evnt] = new List<Action>();
		}

		eventTable[evnt].Add(action);
	}

	public static void RemoveHandler(Event evnt, Action action)
	{
		if (!eventTable.ContainsKey(evnt)) return;

		if (eventTable[evnt] != null)
		{
			eventTable[evnt].Remove(action);
		}

		if (eventTable[evnt] == null || eventTable[evnt].Count == 0)
		{
			eventTable.Remove(evnt);
		}
	}

	/// <summary>
	/// Trigger the given event. If networked, will trigger the event on all clients.
	/// </summary>
	public static void Broadcast(Event evnt, bool network = false)
	{
		LogEventBroadcast(evnt);
		if (eventTable.ContainsKey(evnt) == false || eventTable[evnt] == null) return;

		if (CustomNetworkManager.IsServer && network)
		{
			try
			{
				TriggerEventMessage.SendToAll(evnt);
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}

		}
		else
		{
			for (int i =  eventTable[evnt].Count - 1; i >= 0; i--)
			{
				try
				{
					eventTable[evnt][i]();
				}
				catch (Exception e)
				{
					Loggy.LogError(e.ToString());
				}
			}
		}
	}

	/// <summary>
	/// Calls the appropriate logging category for the event
	/// </summary>
	private static void LogEventBroadcast(Event evnt)
	{
		string msg = "Broadcasting a " + evnt + " event";
		Category category;

		switch (evnt)
		{
			case Event.ChatFocused:
				category = Category.Chat;
				break;
			case Event.ChatUnfocused:
				category = Category.Chat;
				break;
			case Event.UpdateChatChannels:
				category = Category.Chat;
				break;
			case Event.ToggleChatBubbles:
				category = Category.Chat;
				break;
			case Event.UpdateFov:
				category = Category.UI;
				break;
			case Event.DisableInternals:
				category = Category.PlayerInventory;
				break;
			case Event.EnableInternals:
				category = Category.PlayerInventory;
				break;
			case Event.AccountLoggedOut:
				category = Category.DatabaseAPI;
				break;
			case Event.ServerLoggedOut:
				category = Category.Connections;
				break;
			case Event.PlayerRejoined:
				category = Category.Connections;
				break;
			case Event.PlayerSpawned:
				category = Category.EntitySpawn;
				break;
			case Event.PlayerDied:
				category = Category.Health;
				break;
			case Event.GhostSpawned:
				category = Category.Ghosts;
				break;
			case Event.PowerNetSelfCheck:
				category = Category.Electrical;
				break;
			case Event.PreRoundStarted:
				category = Category.Round;
				break;
			case Event.RoundStarted:
				category = Category.Round;
				break;
			case Event.PostRoundStarted:
				category = Category.Round;
				break;
			case Event.RoundEnded:
				category = Category.Round;
				break;
			case Event.BlobSpawned:
				category = Category.Blob;
				break;
			case Event.MatrixManagerInit:
				category = Category.Matrix;
				break;
			default:
				category = Category.Unknown;
				break;
		}


		Loggy.LogTrace(msg, category);


	}

	public void Clear()
	{
		int removed_count = 0;

		foreach (var a in eventTable)
		{
			removed_count += CleanupUtil.RidListOfDeadElements(a.Value);
		}

		Loggy.Log("removing " + removed_count + " dead elements from EventManager.eventTable", Category.MemoryCleanup);
	}
}
