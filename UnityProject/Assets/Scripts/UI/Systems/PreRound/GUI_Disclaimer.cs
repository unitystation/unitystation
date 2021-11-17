using System;
using System.Collections;
using System.Collections.Generic;
using Managers;
using UI.Systems.PreRound;
using UnityEngine;


namespace UI.Systems
{
	public class GUI_Disclaimer : MonoBehaviour
	{
		[SerializeField] private GameObject buttonList;
		[SerializeField] private GameObject MOTDPage;
		[SerializeField] private GameObject eventsPage;
		[SerializeField] private GameObject eventEntry;

		private List<EventEntry> entries = new List<EventEntry>();

		private IEnumerator Start()
		{
			//we do this to avoid the error that happens on the lobby screen on start
			yield return WaitFor.Seconds(2f);
			CheckIfTheresAnEventOnStarting();
		}

		private void CheckIfTheresAnEventOnStarting()
		{
			buttonList.SetActive(true);
			if (TimedEventsManager.Instance.ActiveEvents.Count <= 0)
			{
				MOTDPage.SetActive(true);
				buttonList.SetActive(false);
				eventsPage.SetActive(false);
			}
		}

		public void OnButtonShowEventList()
		{
			MOTDPage.SetActive(false);
			eventsPage.SetActive(true);
			UpdateEventEntryList();
		}
		public void OnButtonShowMOTDPage()
		{
			MOTDPage.SetActive(true);
			eventsPage.SetActive(false);
		}

		private void UpdateEventEntryList()
		{
			foreach (var entry in entries)
			{
				Destroy(entry.gameObject);
			}
			entries.Clear();

			foreach (var eventSo in TimedEventsManager.Instance.ActiveEvents)
			{
				GameObject entry = Instantiate(eventEntry);//creates new button
				entry.SetActive(true);
				var c = entry.GetComponent<EventEntry>();
				c.EventImage.sprite = eventSo.EventIcon;
				c.EventName.text = eventSo.EventName;
				c.EventDesc.text = eventSo.EventDesc;
				entries.Add(c);
				entry.transform.SetParent(eventsPage.transform, false);
			}
		}
	}
}

