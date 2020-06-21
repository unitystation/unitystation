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

		private List<EventScriptBase> listOfEventScripts = new List<EventScriptBase>();

		public List<EventScriptBase> ListOfEventScripts => listOfEventScripts;

		public void AddEventToList(EventScriptBase action)
		{
			ListOfEventScripts.Add(action);
		}

		public void StartRandomEvent()
		{
			foreach (var eventInList in listOfEventScripts)
			{
				var chanceToHappen = UnityEngine.Random.Range(0, 100);

				if (chanceToHappen <= eventInList.ChanceToHappen)
				{
					eventInList.TriggerEvent();
					break;
				}
			}
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.P))
			{
				StartRandomEvent();
			}
		}
	}
}