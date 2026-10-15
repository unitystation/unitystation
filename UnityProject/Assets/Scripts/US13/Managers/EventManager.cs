using System;
using System.Collections.Generic;
using Logs;
using Shared.Util;
using UnityEngine;
using UnityEngine.Events;
using US13.Core.Lifecycle;
using US13.Managers.NetworkManagement;
using US13.Messages.Server;

namespace US13.Managers
{
	public class UIEvent : UnityEvent<GameObject> { }

// For simple broadcasts:
	public enum Event
	{
		UpdateFov, // Not used
		PowerNetSelfCheck, // Not used
		ChatFocused, // Used: Player opens/focuses chat window (ChatUI.cs)
		ChatUnfocused, // Not used
		AccountLoggedOut, // Not used
		ServerLoggedOut, // Not used
		RoundStarted, // Used: Game mode officially starts, players can act (GameMode.cs)
		PostRoundStarted, // Used: Post-round phase begins, results shown (GameManager.cs, LobbyManager.cs)
		SceneUnloading, // Used: Current map/scene is being unloaded (GameManager.cs, LobbyManager.cs)
		RoundEnded, // Used: Round concludes, win/lose conditions met (GameManager.cs, LobbyManager.cs)
		DisableInternals, // Used: Player's internal organs disabled (ControlInternals.cs, PlayerManager.cs)
		EnableInternals, // Not used
		PlayerSpawned, // Not used
		PlayerDied, // Not used
		GhostSpawned, // Not used
		LogLevelAdjusted, // Not used
		UpdateChatChannels, // Used: Available chat channels list updated (PlayerScript.cs)
		ToggleChatBubbles, // Not used
		PlayerRejoined, // Not used
		PreRoundStarted, // Used: Pre-round lobby phase begins (GameManager.cs)
		MatrixManagerInit, // Used: Matrix manager completes initialization (MatrixManager.cs)
		BlobSpawned, // Not used
		ScenesLoadedServer, // Used: All scenes finished loading on server (SubSceneManager.cs)
		LavalandFirstEntered, // Used: Player first enters lavaland via QuantumPad (QuantumPad.cs)
		ChatQuickUnfocus, // Not used
		Cleanup, // Used: Round cleanup phase, clearing entities (GameManager.cs)
		CleanupEnd, // Used: Cleanup phase finished (GameManager.cs)
		ReadyToInitialiseMatrices, // Used: Ready to initialize matrix systems (SubSceneManager.cs)
		GhostRolesInitialized, // Used: Ghost roles assigned to players (GhostRoleManager.cs)
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
					Loggy.Error(e.ToString());
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
						Loggy.Error(e.ToString());
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
				case Event.GhostRolesInitialized:
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


			Loggy.Trace(msg, category);


		}

		public void Clear()
		{
			int removed_count = 0;

			foreach (var a in eventTable)
			{
				removed_count += CleanupUtil.RidListOfDeadElements(a.Value);
			}

			Loggy.Info("removing " + removed_count + " dead elements from EventManager.eventTable", Category.MemoryCleanup);
		}
	}
}