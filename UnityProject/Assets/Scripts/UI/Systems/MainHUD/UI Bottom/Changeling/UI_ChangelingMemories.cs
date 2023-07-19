using Changeling;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
{
	public class UI_ChangelingMemories : MonoBehaviour
	{
		[SerializeField]
		private GameObject entryPrefab = null;

		[SerializeField]
		private GameObject contentArea = null;

		[SerializeField]
		private UI_Changeling ui = null;
		private ChangelingMain changelingMain = null;

		private Dictionary<ChangelingMemories, ChangelingMemoriesEntry> entryPool = new();

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

	public class ChangelingMemories
	{
		[SyncVar] private JobType memoriesJob;
		public JobType MemoriesJob => memoriesJob;
		[SyncVar] private string memoriesName;
		public string MemoriesName => memoriesName;
		[SyncVar] private string memoriesObjectives;
		public string MemoriesObjectives => memoriesObjectives;
		[SyncVar] private string memoriesSpecies;
		public string MemoriesSpecies => memoriesSpecies;
		[SyncVar] private Gender memoriesGender;
		public Gender MemoriesGender => memoriesGender;

		public void Form(JobType job, string playerName, string objectives, string species, Gender gender)
		{
			memoriesJob = job;
			memoriesName = playerName;
			memoriesObjectives = objectives;
			memoriesSpecies = species;
			memoriesGender = gender;
		}
	}
}