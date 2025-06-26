using System.Collections.Generic;
using Mirror;
using Shared.Managers;
using UnityEngine;

namespace Actions.V2.UI
{
	public class ActionButtonManager : SingletonManager<ActionButtonManager>
	{
		public GameObject ActionButtonPrefab;

		private List<UIActionButton> spawnedButtons = new List<UIActionButton>();

		public void RefreshButtons(SyncList<ActionButtonData> actionButtons)
		{
			var trackedItems = new List<string>();
			var itemsToRemove = new List<string>();
			foreach (var buttonData in actionButtons)
			{
				trackedItems.Add(buttonData.ID);
				Debug.Log($"Refreshing button: {buttonData.DisplayName} with ID: {buttonData.ID}");
				if (spawnedButtons.Exists(b => b.name == buttonData.ID))
				{
					continue;
				}
				// Create a new button if it doesn't exist
				var newButton = Instantiate(ActionButtonPrefab, transform);
				var logic = newButton.GetComponent<UIActionButton>();
				logic.Setup(buttonData);
				spawnedButtons.Add(logic);
			}

			foreach (var b in spawnedButtons)
			{
				if (trackedItems.Contains(b.name)) continue;
				itemsToRemove.Add(b.name);
			}

			spawnedButtons.RemoveAll(x =>
			{
				var shouldRemove = itemsToRemove.Contains(x.name);
				if (shouldRemove)
				{
					Destroy(x.gameObject);
				}
				return shouldRemove;
			});
		}
	}
}