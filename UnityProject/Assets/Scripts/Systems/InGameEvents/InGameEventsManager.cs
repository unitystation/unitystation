using DiscordWebhook;
using GameConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace InGameEvents
{
	/// <summary>
	/// The controller for in game events
	/// </summary>
	public class InGameEventsManager : MonoBehaviour
	{
		private static InGameEventsManager instance;
		public static InGameEventsManager Instance => instance;

		private float timer = 0f;

		/// <summary>
		/// How long between each event check
		/// </summary>
		[SerializeField]
		private float triggerEventInterval = 600f;

		/// <summary>
		/// Chance the random event is fake as a %
		/// </summary>
		[SerializeField]
		private int chanceItIsFake = 25;

		public bool RandomEventsAllowed;

		public int minPlayersForRandomEventsToHappen = 5;

		[HideInInspector]
		public List<string> EnumListCache = new List<string>();


		private void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
			else
			{
				Destroy(this);
			}

			EnumListCache = Enum.GetNames(typeof(InGameEventType)).ToList();
		}

		public void Start()
		{
			RandomEventsAllowed = GameConfigManager.GameConfig.RandomEventsAllowed;
		}

		private void Update()
		{
			if (!CustomNetworkManager.IsServer) return;

			if (!RandomEventsAllowed) return;

			if(GameManager.Instance.CurrentRoundState != RoundState.Started) return;

			if(PlayerList.Instance.InGamePlayers.Count < minPlayersForRandomEventsToHappen) return;

			timer += Time.deltaTime;
			if (timer > triggerEventInterval)
			{
				var isFake = Random.Range(0,100) < chanceItIsFake;

				StartRandomEvent(GetRandomEventList(), isFake: isFake, serverTriggered: true);

				timer -= triggerEventInterval;
			}
		}

		private List<EventScriptBase> listOfFunEventScripts = new List<EventScriptBase>();
		public List<EventScriptBase> ListOfFunEventScripts => listOfFunEventScripts;


		private List<EventScriptBase> listOfSpecialEventScripts = new List<EventScriptBase>();
		public List<EventScriptBase> ListOfSpecialEventScripts => listOfSpecialEventScripts;


		private List<EventScriptBase> listOfAntagonistEventScripts = new List<EventScriptBase>();
		public List<EventScriptBase> ListOfAntagonistEventScripts => listOfAntagonistEventScripts;


		private List<EventScriptBase> listOfDebugEventScripts = new List<EventScriptBase>();
		public List<EventScriptBase> ListOfDebugEventScripts => listOfDebugEventScripts;

		public void AddEventToList(EventScriptBase eventToAdd, InGameEventType eventType)
		{
			var list = GetListFromEnum(eventType);

			if (list == null)
			{
				Debug.LogError("An event has been set to random type, random is a dummy type and cant be accessed.");
				return;
			}

			if (list.Contains(eventToAdd)) return;

			list.Add(eventToAdd);
		}

		public void TriggerSpecificEvent(int eventIndex, InGameEventType eventType, bool isFake = false, string adminName = null, bool announceEvent = true, string serializedEventParameters = null)
		{
			List<EventScriptBase> list;

			if (eventType == InGameEventType.Random)
			{
				list = GetRandomEventList();
			}
			else
			{
				list = GetListFromEnum(eventType);
			}

			if (list == null)
			{
				Debug.LogError("Event List was null shouldn't happen unless new type wasn't added to switch");
				return;
			}

			if (eventIndex == 0)
			{
				StartRandomEvent(list, true, isFake, false, adminName, announceEvent);
			}
			else
			{
				var eventChosen = list[eventIndex - 1];
				eventChosen.FakeEvent = isFake;
				eventChosen.AnnounceEvent = announceEvent;
				eventChosen.TriggerEvent(serializedEventParameters);

				var msg = $"{adminName}: triggered the event: {eventChosen.EventName}. Is fake: {isFake}. Announce: {announceEvent}";

				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
				DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
			}
		}

		public void StartRandomEvent(List<EventScriptBase> eventList, bool anEventMustHappen = false, bool isFake = false, bool serverTriggered = false, string adminName = null, bool announceEvent = true, int stackOverFlowProtection = 0)
		{
			if (eventList.Count == 0) return;

			foreach (var eventInList in eventList.Shuffle())
			{
				//If there's not enough players try to trigger a different one
				if(eventInList.MinPlayersToTrigger > PlayerList.Instance.InGamePlayers.Count) continue;

				var chanceToHappen = UnityEngine.Random.Range(0f, 100f);

				if (chanceToHappen < eventInList.ChanceToHappen)
				{
					eventInList.FakeEvent = isFake;
					eventInList.AnnounceEvent = announceEvent;
					eventInList.TriggerEvent();

					string msg;

					if (serverTriggered)
					{
						msg = $"A random event, {eventInList.EventName} has been triggered by the server. Is fake: {isFake}. Announce: {announceEvent}";

						UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
						DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "[Server]");
						return;
					}

					msg = $"{adminName}: triggered a random event, {eventInList.EventName} was chosen. Is fake: {isFake}. Announce: {announceEvent}";

					UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
					DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");

					return;
				}
			}

			stackOverFlowProtection++;

			if (stackOverFlowProtection > 100)
			{
				var msg = "A random event has failed to be triggered by the server due to overflow protection.";

				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
				DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "[Server]");
				return;
			}

			if (anEventMustHappen)
			{
				StartRandomEvent(eventList, true, isFake, serverTriggered, adminName, announceEvent, stackOverFlowProtection);
			}
		}

		public List<EventScriptBase> GetListFromEnum(InGameEventType enumValue)
		{
			switch (enumValue)
			{
				case InGameEventType.Random:
					return null;
				case InGameEventType.Fun:
					return ListOfFunEventScripts;
				case InGameEventType.Special:
					return ListOfSpecialEventScripts;
				case InGameEventType.Antagonist:
					return ListOfAntagonistEventScripts;
				case InGameEventType.Debug:
					return ListOfDebugEventScripts;
				default: return null;
			}
		}

		public void RemoveEventFromList(EventScriptBase eventToRemove, InGameEventType enumValue)
		{
			switch (enumValue)
			{
				case InGameEventType.Fun:
					ListOfFunEventScripts.Remove(eventToRemove);
					return;
				case InGameEventType.Special:
					ListOfSpecialEventScripts.Remove(eventToRemove);
					return;
				case InGameEventType.Antagonist:
					ListOfAntagonistEventScripts.Remove(eventToRemove);
					return;
				case InGameEventType.Debug:
					ListOfDebugEventScripts.Remove(eventToRemove);
					return;
			}
		}

		public List<EventScriptBase> GetRandomEventList()
		{
			var enumList = EnumListCache;
			enumList.Remove("Random");
			enumList.Remove("Debug");

			if (InGameEventType.TryParse(enumList.GetRandom(), out InGameEventType result))
			{
				return GetListFromEnum(result);
			}
			return null;
		}
	}
}