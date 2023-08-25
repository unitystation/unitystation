using Changeling;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
{
	public class UiChangelingMemories : MonoBehaviour
	{
		[SerializeField]
		private GameObject entryPrefab = null;

		[SerializeField]
		private GameObject contentArea = null;

		private ChangelingMain changelingMain = null;

		private readonly Dictionary<ChangelingMemories, ChangelingMemoriesEntry> entryPool = new();

		public void Refresh(List<ChangelingMemories> toBuy, ChangelingMain changeling)
		{
			changelingMain = changeling;
			Clear();
			ShowMemories(toBuy);
		}

		private void ShowMemories(List<ChangelingMemories> toBuy)
		{
			foreach (var x in toBuy)
			{
				AddEntry(x);
			}
		}

		public void Clear()
		{
			foreach (var entry in entryPool)
			{
				RemoveEntry(entry.Key);
			}
			entryPool.Clear();
		}

		private void RemoveEntry(ChangelingMemories dataForRemovingEntry)
		{
			Destroy(entryPool[dataForRemovingEntry].gameObject);
		}

		private void AddEntry(ChangelingMemories dataForCreatingEntry)
		{
			if (entryPool.ContainsKey(dataForCreatingEntry))
				return;
			entryPrefab.SetActive(true);
			var newEntry = Instantiate(entryPrefab, contentArea.transform).GetComponent<ChangelingMemoriesEntry>();
			entryPrefab.SetActive(false);
			newEntry.Init(this, dataForCreatingEntry, changelingMain);
			entryPool.Add(dataForCreatingEntry, newEntry);
		}
	}
}