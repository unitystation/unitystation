using System;
using System.Collections.Generic;
using System.Linq;
using Systems.Antagonists;
using UnityEngine;

namespace UI.Systems.MainHUD.UI_Bottom
{
	public class UI_AlienHiveMenu : MonoBehaviour
	{
		[SerializeField]
		private GameObject entryPrefab = null;

		[SerializeField]
		private GameObject contentArea = null;

		private List<HiveMenuEntry> entryPool = new List<HiveMenuEntry>();

		private void OnEnable()
		{
			Refresh();
		}

		public void Refresh()
		{
			var aliens = FindObjectsOfType<AlienPlayer>().Where(x => x.IsDead == false).ToArray();

			if (entryPool.Count < aliens.Length)
			{
				var missing = aliens.Length - entryPool.Count;
				for (int i = 0; i <= missing; i++)
				{
					AddEntry();
				}
			}

			if (entryPool.Count > aliens.Length)
			{
				var missing = entryPool.Count - aliens.Length;
				for (int i = 0; i <= missing; i++)
				{
					RemoveEntry();
				}
			}

			for (int i = 0; i < aliens.Length; i++)
			{
				var alien = aliens[i];
				entryPool[i].SetUp(alien.RegisterPlayer.PlayerScript.playerName, alien.CurrentData.Normal.Variance[0].Frames[0].sprite);
			}
		}

		private void AddEntry()
		{
			var newEntry = Instantiate(entryPrefab, contentArea.transform).GetComponent<HiveMenuEntry>();
			entryPool.Add(newEntry);
		}

		private void RemoveEntry()
		{
			entryPool.RemoveAt(entryPool.Count - 1);
		}
	}
}