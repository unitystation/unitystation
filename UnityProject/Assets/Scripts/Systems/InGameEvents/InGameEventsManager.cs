using System;
using System.Collections.Generic;
using System.Linq;
using AdminCommands;
using GameConfig;
using Logs;
using Shared.Managers;
using Systems.Score;
using UnityEngine;
using Random = UnityEngine.Random;

namespace InGameEvents
{
	/// <summary>
	/// The controller for in game events
	/// </summary>
	public class InGameEventsManager : SingletonManager<InGameEventsManager>
	{
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

		[SerializeField] private int scoreForSpawningEvents = 25;

		public bool RandomEventsAllowed;

		public int minPlayersForRandomEventsToHappen = 5;

		[HideInInspector]
		public List<string> EnumListCache = new List<string>();


		public override void Awake()
		{
			base.Awake();
			EnumListCache = Enum.GetNames(typeof(InGameEventType)).ToList();
		}

		public override void Start()
		{
			base.Start();
			RandomEventsAllowed = GameConfigManager.GameConfig.RandomEventsAllowed;
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
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
				Loggy.LogError("An event has been set to random type, random is a dummy type and cant be accessed.", Category.Event);
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
				Loggy.LogError("Event List was null shouldn't happen unless new type wasn't added to switch", Category.Event);
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

				AdminCommandsManager.LogAdminAction($"{adminName}: triggered the event: {eventChosen.EventName}. Is fake: {isFake}. Announce: {announceEvent}");
			}
		}

		public void TriggerSpecificEvent(string EventName, bool isFake = false, bool announceEvent = true, string serializedEventParameters = null)
		{
			var Event = ListOfFunEventScripts.FirstOrDefault(x => x.EventName == EventName);
			if (Event == null)
			{
				Event = listOfSpecialEventScripts.FirstOrDefault(x => x.EventName == EventName);
			}

			if (Event == null)
			{
				Event = listOfAntagonistEventScripts.FirstOrDefault(x => x.EventName == EventName);
			}

			if (Event == null)
			{
				Event = listOfDebugEventScripts.FirstOrDefault(x => x.EventName == EventName);
			}

			if (Event == null)
			{
				Loggy.LogError($"Unable to find event {EventName}, Make sure it set up properly inside of In game event manager prefab, And the name is exactly copied from the field EventName");
				return;
			}

			Event.FakeEvent = isFake;
			Event.AnnounceEvent = announceEvent;
			Event.TriggerEvent(serializedEventParameters);

			AdminCommandsManager.LogAdminAction($"GameCode: triggered the event: {Event.EventName}. Is fake: {isFake}. Announce: {announceEvent}");

		}

		public void StartRandomEvent(List<EventScriptBase> eventList, bool anEventMustHappen = false, bool isFake = false, bool serverTriggered = false, string adminName = null, bool announceEvent = true, int stackOverFlowProtection = 0)
		{
			if (eventList.Count == 0) return;

			var ToLoop = eventList.Where(x => x.CanRandomlyTrigger).Shuffle();
			foreach (var eventInList in ToLoop)
			{
				//If there's not enough players try to trigger a different one
				if(eventInList.MinPlayersToTrigger > PlayerList.Instance.InGamePlayers.Count) continue;

				var chanceToHappen = Random.Range(0f, 100f);

				if (chanceToHappen < eventInList.ChanceToHappen)
				{
					eventInList.FakeEvent = isFake;
					eventInList.AnnounceEvent = announceEvent;
					eventInList.TriggerEvent();
					ScoreMachine.AddToScoreInt(scoreForSpawningEvents, RoundEndScoreBuilder.COMMON_SCORE_RANDOMEVENTSTRIGGERED);

					if (serverTriggered)
					{
						AdminCommandsManager.LogAdminAction($"A random event, {eventInList.EventName} has been triggered by the server. Is fake: {isFake}. Announce: {announceEvent}", "[Server]");
						return;
					}

					AdminCommandsManager.LogAdminAction($"{adminName}: triggered a random event, {eventInList.EventName} was chosen. Is fake: {isFake}. Announce: {announceEvent}");

					return;
				}
			}

			stackOverFlowProtection++;

			if (stackOverFlowProtection > 100)
			{
				AdminCommandsManager.LogAdminAction("A random event has failed to be triggered by the server due to overflow protection.", "[Server]");
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