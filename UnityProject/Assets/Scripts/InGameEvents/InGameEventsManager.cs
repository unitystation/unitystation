using System.Collections;
using System.Collections.Generic;
using AdminTools;
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

		public bool RandomEventsAllowed = true;


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
			if (!CustomNetworkManager.IsServer) return;

			if (RandomEventsAllowed && GameManager.Instance.CurrentRoundState == RoundState.Started )
			{
				timer += Time.deltaTime;
				if (timer > triggerEventInterval)
				{
					bool isFake = false;

					if (UnityEngine.Random.Range(0,100) < chanceItIsFake)
					{
						isFake = true;
					}

					StartRandomFunEvent(isFake: isFake, serverTriggered: true);

					timer -= triggerEventInterval;
				}
			}
		}

		private List<EventScriptBase> listOfFunEventScripts = new List<EventScriptBase>();

		public List<EventScriptBase> ListOfFunEventScripts => listOfFunEventScripts;

		public void AddEventToList(EventScriptBase eventToAdd)
		{
			listOfFunEventScripts.Add(eventToAdd);
		}

		public void TriggerSpecificEvent(int eventIndex, bool isFake = false, string adminName = null, bool announceEvent = true)
		{
			if (eventIndex == 0)
			{
				StartRandomFunEvent(true, isFake, false, adminName, announceEvent);
			}
			else
			{
				var eventChosen = listOfFunEventScripts[eventIndex - 1];
				eventChosen.FakeEvent = isFake;
				eventChosen.AnnounceEvent = announceEvent;
				eventChosen.TriggerEvent();

				var msg = $"{adminName}: triggered a random event, {eventChosen.EventName} was chosen. Is fake: {isFake}. Announce: {announceEvent}";

				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
				DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
			}
		}

		public void StartRandomFunEvent(bool anEventMustHappen = false, bool isFake = false, bool serverTriggered = false, string adminName = null, bool announceEvent = true)
		{
			foreach (var eventInList in listOfFunEventScripts.Shuffle())
			{
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

			if (anEventMustHappen)
			{
				StartRandomFunEvent(true, isFake, serverTriggered, adminName, announceEvent);
			}
		}
	}
}