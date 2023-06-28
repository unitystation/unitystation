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
		private UI_Changeling uiAlien = null;

		private List<EvolveMenuEntry> entryPool = new List<EvolveMenuEntry>();


		public void Refresh()
		{

		}

		private void AddEntry()
		{
			entryPrefab.SetActive(true);
			var newEntry = Instantiate(entryPrefab, contentArea.transform).GetComponent<EvolveMenuEntry>();
			entryPrefab.SetActive(false);
			entryPool.Add(newEntry);
		}

		private void RemoveEntry()
		{
			Destroy(entryPool[^1]);

			entryPool.RemoveAt(entryPool.Count - 1);
		}
	}
}