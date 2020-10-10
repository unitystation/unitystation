using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdminTools
{
	public class PlayerAlertsScroll : ChatScroll
	{
		private List<PlayerAlertData> alertLog = new List<PlayerAlertData>();

		public void LoadAlertEntries(List<PlayerAlertData> alertsToLoad)
		{
			ReturnAllViewsToPool();

			alertLog = new List<PlayerAlertData>(alertsToLoad);
			if (gameObject.activeInHierarchy)
			{
				StartCoroutine(LoadAllPlayerAlerts());
			}
		}

		public void AppendAlertEntries(List<PlayerAlertData> alertsToAppend)
		{
			foreach (var e in alertsToAppend)
			{
				AddNewPlayerAlert(e);
			}
		}

		public void UpdateExistingPlayerAlert(PlayerAlertData alertEntry)
		{
			var index = alertLog.FindIndex(x =>
				x.playerNetId == alertEntry.playerNetId && x.roundTime == alertEntry.roundTime);

			if (index == -1)
			{
				AddNewPlayerAlert(alertEntry);
			}
			else
			{
				foreach (var v in displayPool.Cast<PlayerAlertView>().ToList())
				{
					if (v.LoadedData == alertLog[index])
					{
						alertLog[index] = alertEntry;
						v.Reload(alertEntry);
						return;
					}
				}

				alertLog[index] = alertEntry;
			}
		}

		public void AddNewPlayerAlert(PlayerAlertData alertEntry)
		{
			if (displayPool.Count != 0 && displayPool[0].Index != alertLog.Count - 1)
			{
				alertLog.Add(alertEntry);
				return;
			}
			alertLog.Add(alertEntry);
			TryShowView(alertEntry, true, alertLog.Count - 1);
		}

		IEnumerator LoadAllPlayerAlerts()
		{
			while (!isInit)
			{
				yield return WaitFor.EndOfFrame;
			}

			var count = 0;
			for (int i = alertLog.Count - 1; i >= 0; i--)
			{
				TryShowView(alertLog[i], false, i);

				count++;
				if (count == MaxViews) break;
			}
		}

		public override void ReturnAllViewsToPool()
		{
			base.ReturnAllViewsToPool();
			alertLog.Clear();
		}

		public override void OnScrollUp()
		{
			if (alertLog.Count <= MaxViews) return;
			if (displayPool.Count == 0) return;


			//Player wants to see chat entries further up
			//get the data index of the view at the top of the display list
			var index = displayPool[displayPool.Count - 1].Index;
			//check to see if we can scroll any further up
			if (index == 0) return;
			//if so then spawn a new view at the top and remove on from the bottom
			var proposedIndex = index - 1;
			TryShowView(alertLog[proposedIndex], false, proposedIndex, ScrollButtonDirection.Down);
			ReturnViewToPool(displayPool[0]);
		}

		public override void OnScrollDown()
		{
			if (alertLog.Count <= MaxViews) return;
			if (displayPool.Count == 0) return;

			//Player wants to see chat entries further down
			//get the data index of the view on the bottom
			var index = displayPool[0].Index;

			//check to see if we can scroll any further based off this index
			if (index == alertLog.Count - 1) return;
			//if so then spawn a new view at the bottom and remove one from the top
			var proposedIndex = index + 1;

			TryShowView(alertLog[proposedIndex], true, proposedIndex, ScrollButtonDirection.Down);
			ReturnViewToPool(displayPool[displayPool.Count - 1]);
		}
	}
}
