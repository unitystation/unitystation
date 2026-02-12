using System.Collections;
using TMPro;
using UnityEngine;
using US13.UI.Systems.ServerInfoPanel.Models;
using Util;

namespace US13.UI.Systems.ServerInfoPanel
{
	public class BuildEntry: MonoBehaviour
	{
		[SerializeField] private GameObject changeEntryPrefab;
		[SerializeField] private TMP_Text buildVersion;
		[SerializeField] private TMP_Text buildDate;

		[SerializeField] private Transform changesContainer;

		private const int CHUNK_SIZE = 10;

		public void SetBuild(Build build)
		{
			buildVersion.text = build.version_number;
			buildDate.text = build.date_created;

			StartCoroutine(SpawnPrefabs(build));
		}

		private IEnumerator SpawnPrefabs(Build build)
		{
			// breaks the changes into chunks of 10
			foreach (var chunk in build.changes.Chunk(CHUNK_SIZE))
			{
				foreach (var change in chunk)
				{
					var changeEntry = Instantiate(changeEntryPrefab, changesContainer);
					changeEntry.GetComponent<ChangeEntry>().SetChange(change);
				}
				// waits for the next frame to keep spawning
				yield return null;
			}

		}
	}
}