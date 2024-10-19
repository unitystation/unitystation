using System;
using System.Collections.Generic;
using System.Linq;
using Systems.Antagonists;
using TMPro;
using UnityEngine;

namespace UI.Systems.MainHUD.UI_Bottom
{
	public class UI_AlienHiveMenu : MonoBehaviour
	{
		[SerializeField]
		private GameObject entryPrefab = null;

		[SerializeField]
		private GameObject contentArea = null;

		[SerializeField]
		private TMP_Text hiveMembersText = null;

		private List<HiveMenuEntry> entryPool = new List<HiveMenuEntry>();

		private void AddEntry()
		{
			entryPrefab.SetActive(true);
			var newEntry = Instantiate(entryPrefab, contentArea.transform).GetComponent<HiveMenuEntry>();
			entryPrefab.SetActive(false);
			entryPool.Add(newEntry);
		}

		private void RemoveEntry()
		{
			entryPool.RemoveAt(entryPool.Count - 1);
		}
	}
}