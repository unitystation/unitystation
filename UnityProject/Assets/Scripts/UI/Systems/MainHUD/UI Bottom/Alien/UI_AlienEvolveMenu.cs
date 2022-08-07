using System.Collections.Generic;
using System.Linq;
using Systems.Antagonists;
using UnityEngine;

namespace UI.Systems.MainHUD.UI_Bottom
{
	public class UI_AlienEvolveMenu : MonoBehaviour
	{
		[SerializeField]
		private GameObject entryPrefab = null;

		[SerializeField]
		private GameObject contentArea = null;

		[SerializeField]
		private UI_Alien uiAlien = null;

		private List<EvolveMenuEntry> entryPool = new List<EvolveMenuEntry>();

		private void OnEnable()
		{
			Refresh();
		}

		public void Refresh()
		{
			if(uiAlien.AlienPlayer == null) return;

			var alienEvolutions = uiAlien.AlienPlayer.TypesToChoose.Where(x => x.EvolvedFrom.HasFlag(uiAlien.AlienPlayer.CurrentAlienType)).ToArray();

			if (entryPool.Count < alienEvolutions.Length)
			{
				var missing = alienEvolutions.Length - entryPool.Count;
				for (int i = 0; i < missing; i++)
				{
					AddEntry();
				}
			}

			if (entryPool.Count > alienEvolutions.Length)
			{
				var missing = entryPool.Count - alienEvolutions.Length;
				for (int i = 0; i < missing; i++)
				{
					RemoveEntry();
				}
			}

			for (int i = 0; i < alienEvolutions.Length; i++)
			{
				var alien = alienEvolutions[i];
				entryPool[i].SetUp(alien.Name, alien.Description,
					alien.Normal.Variance[0].Frames[0].sprite, uiAlien, alien.AlienType);
			}
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