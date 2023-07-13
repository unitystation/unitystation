using System;
using System.Collections;
using System.Collections.Generic;
using UI.Systems.MainHUD.UI_Bottom;
using UnityEngine;

namespace Changeling
{
	public class UI_ChangelingStore : MonoBehaviour
	{
		[SerializeField]
		private GameObject entryPrefab = null;

		[SerializeField]
		private GameObject contentArea = null;

		[SerializeField]
		private GameObject resetButton = null;

		[SerializeField]
		private UI_Changeling ui = null;
		private ChangelingMain changelingMain = null;

		private Dictionary<ChangelingData, ChangelingAbilityEntry> entryPool = new ();

		public void Refresh(List<ChangelingData> toBuy, ChangelingMain changeling)
		{
			changelingMain = changeling;
			Clear();
			ShowAbilities(toBuy);
		}

		private void ShowAbilities(List<ChangelingData> toBuy)
		{
			foreach (var x in toBuy)
			{
				if (x.ShowInStore)
					AddEntry(x);
			}
			resetButton.SetActive(changelingMain.ResetsLeft > 0);
		}

		public void Clear()
		{
			foreach (var entry in entryPool)
			{
				RemoveEntry(entry.Key);
			}
			entryPool.Clear();
		}

		private void AddEntry(ChangelingData dataForCreatingEntry)
		{
			if (entryPool.ContainsKey(dataForCreatingEntry))
				return;
			entryPrefab.SetActive(true);
			var newEntry = Instantiate(entryPrefab, contentArea.transform).GetComponent<ChangelingAbilityEntry>();
			entryPrefab.SetActive(false);
			newEntry.Init(this, dataForCreatingEntry, changelingMain);
			entryPool.Add(dataForCreatingEntry, newEntry);
		}

		private void RemoveEntry(ChangelingData dataForRemovingEntry)
		{
			Destroy(entryPool[dataForRemovingEntry].gameObject);
		}

		public void ResetAbilites()
		{
			ui.ResetAbilites();
			resetButton.SetActive(changelingMain.ResetsLeft > 0);
		}

		public void AddAbility(ChangelingData data)
		{
			//RemoveEntry(data);
			ui.AddAbility(data);
		}
	}
}