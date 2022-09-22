using System.Collections;
using TMPro;
using UI.Systems.ServerInfoPanel.Models;
using UnityEngine;

namespace UI.Systems.ServerInfoPanel
{
	public class BuildEntry: MonoBehaviour
	{
		[SerializeField] private GameObject changeEntryPrefab;
		[SerializeField] private TMP_Text buildVersion;
		[SerializeField] private TMP_Text buildDate;

		[SerializeField] private Transform changesContainer;

		public void SetBuild(Build build)
		{
			buildVersion.text = build.version_number;
			buildDate.text = build.date_created;

			StartCoroutine(SpawnPrefabs(build));
		}

		private IEnumerator SpawnPrefabs(Build build)
		{
			// breaks the changes into chunks of 10
			foreach (var chunk in build.changes.Chunk(10))
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