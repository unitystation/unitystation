using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DiscordWebhook;

namespace InGameEvents
{
	/// <summary>
	/// The controller for in game events
	/// </summary>
	public class InGameEventsManager : MonoBehaviour
	{
		private static InGameEventsManager instance;
		public static InGameEventsManager Instance => instance;

		private float Timer = 0f;

		/// <summary>
		/// How long between each event check
		/// </summary>
		[SerializeField]
		private float TriggerEventInterval = 600f;

		/// <summary>
		/// Chance the random event is fake as a %
		/// </summary>
		[SerializeField]
		private int ChanceItIsFake = 25;


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
		}

		private void Update()
		{
			if (GameManager.Instance.CurrentRoundState == RoundState.Started)
			{
				Timer += Time.deltaTime;
				if (Timer > TriggerEventInterval)
				{
					bool isFake = false;

					if (UnityEngine.Random.Range(0,100) < ChanceItIsFake)
					{
						isFake = true;
					}

					StartRandomFunEvent(isFake: isFake, serverTriggered: true);

					Timer -= TriggerEventInterval;
				}
			}
		}

		private List<EventScriptBase> listOfFunEventScripts = new List<EventScriptBase>();

		public List<EventScriptBase> ListOfFunEventScripts => listOfFunEventScripts;

		public void AddEventToList(EventScriptBase eventToAdd)
		{
			listOfFunEventScripts.Add(eventToAdd);
		}

		public void TriggerSpecificEvent(int eventIndex, bool isFake = false, string AdminName = null, bool announceEvent = true)
		{
			if (eventIndex == 0)
			{
				StartRandomFunEvent(true, isFake, AdminName: AdminName, announceEvent: announceEvent);
			}
			else
			{
				var eventChosen = listOfFunEventScripts[eventIndex - 1];
				eventChosen.FakeEvent = isFake;
				eventChosen.AnnounceEvent = announceEvent;
				eventChosen.TriggerEvent();
				DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, $"{AdminName}: triggered a random event, {eventChosen.EventName} was choosen. Is fake: {isFake}", "");
			}
		}

		public void StartRandomFunEvent(bool AnEventMustHappen = false, bool isFake = false, bool serverTriggered = false, string AdminName = null, bool announceEvent = true)
		{
			foreach (var eventInList in listOfFunEventScripts.Shuffle())
			{
				var chanceToHappen = UnityEngine.Random.Range(0, 100);

				if (chanceToHappen <= eventInList.ChanceToHappen)
				{
					eventInList.FakeEvent = isFake;
					eventInList.AnnounceEvent = announceEvent;
					eventInList.TriggerEvent();

					if (serverTriggered)
					{
						DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, $"A random event, {eventInList.EventName} has been triggered by the server. Is fake: {isFake}", "[Server]");
						return;
					}

					DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, $"{AdminName}: triggered a random event, {eventInList.EventName} was choosen. Is fake: {isFake}", "");

					return;
				}
			}

			if (AnEventMustHappen)
			{
				StartRandomFunEvent(true, isFake);
			}
		}
	}
}