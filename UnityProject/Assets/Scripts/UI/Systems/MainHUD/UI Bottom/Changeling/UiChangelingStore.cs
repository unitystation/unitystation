using System;
using System.Collections;
using System.Collections.Generic;
using UI.Systems.MainHUD.UI_Bottom;
using UnityEngine;

namespace Changeling
{
	public class UiChangelingStore : MonoBehaviour
	{
		[SerializeField]
		private GameObject entryPrefab = null;

		[SerializeField]
		private GameObject contentArea = null;

		[SerializeField]
		private GameObject resetButton = null;

		[SerializeField]
		private UiChangeling ui = null;
		public UiChangeling Ui => ui;
		private ChangelingMain changelingMain = null;

		private readonly Dictionary<ChangelingBaseAbility, ChangelingAbilityEntry> entryPool = new ();

		public void Refresh(List<ChangelingBaseAbility> toBuy, ChangelingMain changeling)
		{
			changelingMain = changeling;
			Clear();
			ShowAbilities(toBuy);
		}

		private void ShowAbilities(List<ChangelingBaseAbility> toBuy)
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

		private void AddEntry(ChangelingBaseAbility dataForCreatingEntry)
		{
			if (entryPool.ContainsKey(dataForCreatingEntry))
				return;
			entryPrefab.SetActive(true);
			var newEntry = Instantiate(entryPrefab, contentArea.transform).GetComponent<ChangelingAbilityEntry>();
			entryPrefab.SetActive(false);
			newEntry.Init(this, dataForCreatingEntry, changelingMain);
			entryPool.Add(dataForCreatingEntry, newEntry);
		}

		private void RemoveEntry(ChangelingBaseAbility dataForRemovingEntry)
		{
			Destroy(entryPool[dataForRemovingEntry].gameObject);
		}

		public void ResetAbilites()
		{
			if (changelingMain == null)
				return;
			ui.ResetAbilites();
			resetButton.SetActive(changelingMain.ResetsLeft > 0);
		}

		public void UpdateResetButton()
		{
			if (changelingMain == null)
				return;
			resetButton.SetActive(changelingMain.ResetsLeft > 0);
		}

		public void AddAbility(ChangelingBaseAbility data)
		{
			ui.AddAbility(data);
		}
	}
}