using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using US13.Systems.Antagonists;

namespace US13.UI.Systems.MainHUD.UI_Bottom.Alien
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

		private void OnEnable()
		{
			Refresh();
		}

		public void Refresh()
		{
			var aliens = FindObjectsOfType<AlienPlayer>().Where(x => x.IsDead == false).OrderByDescending(x => x.AlienType.AlienType).ToArray();

			hiveMembersText.text = $"There {(aliens.Length == 1 ? "is" : "are")} {aliens.Length} hive sister{(aliens.Length > 1 ? "s" : "")}";

			if (entryPool.Count < aliens.Length)
			{
				var missing = aliens.Length - entryPool.Count;
				for (int i = 0; i < missing; i++)
				{
					AddEntry();
				}
			}

			if (entryPool.Count > aliens.Length)
			{
				var missing = entryPool.Count - aliens.Length;
				for (int i = 0; i < missing; i++)
				{
					RemoveEntry();
				}
			}

			for (int i = 0; i < aliens.Length; i++)
			{
				var alien = aliens[i];
				entryPool[i].SetUp(alien.RegisterPlayer.PlayerScript.playerName, alien.AlienType.Normal.GetFirstSprite);
			}
		}

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