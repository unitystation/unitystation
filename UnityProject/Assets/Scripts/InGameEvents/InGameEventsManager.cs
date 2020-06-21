using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGameEvents
{
	/// <summary>
	/// The controller for in game events
	/// </summary>
	public class InGameEventsManager : MonoBehaviour
	{
		private static InGameEventsManager instance;
		public static InGameEventsManager Instance => instance;


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

		private List<EventScriptBase> listOfFunEventScripts = new List<EventScriptBase>();

		public List<EventScriptBase> ListOfFunEventScripts => listOfFunEventScripts;

		public void AddEventToList(EventScriptBase eventToAdd)
		{
			listOfFunEventScripts.Add(eventToAdd);
		}

		public void TriggerSpecificEvent(int eventIndex, bool isFake = false)
		{
			if (eventIndex == 0)
			{
				StartRandomFunEvent(true, isFake);
			}
			else
			{
				var eventChosen = listOfFunEventScripts[eventIndex - 1];
				eventChosen.FakeEvent = isFake;
				eventChosen.TriggerEvent();
			}
		}

		public void StartRandomFunEvent(bool AnEventMustHappen = false, bool isFake = false)
		{
			foreach (var eventInList in listOfFunEventScripts)
			{
				var chanceToHappen = UnityEngine.Random.Range(0, 100);

				if (chanceToHappen <= eventInList.ChanceToHappen)
				{
					eventInList.FakeEvent = isFake;
					eventInList.TriggerEvent();
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