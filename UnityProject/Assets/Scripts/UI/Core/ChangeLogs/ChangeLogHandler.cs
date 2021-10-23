using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
	public class ChangeLogHandler : MonoBehaviour
	{
		public GameObject window;
		public Transform content;
		private List<ChangeLogEntry> allEntries = new List<ChangeLogEntry>();
		public GameObject entryPrefab;

		void Start()
		{
			window.SetActive(false);

			return;
			// FIXME: broken on steam
			// Commented out because the following unreachable code would cause a compiler warning.
			//
			//string filePath = Path.Combine(Application.streamingAssetsPath, "changelog.json");
			//
			//if (File.Exists(filePath))
			//{
			//    string data = File.ReadAllText(filePath);
			//    allEntries = JsonConvert.DeserializeObject<List<ChangeLogEntry>>(data);
			//    OpenWindow();
			//}
		}

		void OpenWindow()
		{
			// Populate results
			window.SetActive(true);
			foreach (ChangeLogEntry entry in allEntries)
			{
				GameObject obj = Instantiate(entryPrefab, Vector3.one, Quaternion.identity);
				obj.transform.parent = content;
				obj.transform.localScale = Vector3.one;

				ChangeLogEntryUI ui = obj.GetComponent<ChangeLogEntryUI>();
				ui.SetEntry(entry);
			}
		}

		public void CloseWindow()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			window.SetActive(false);
		}
	}

	[Serializable]
	public class ChangeLogEntry
	{
		public string date;
		public string commit;
		public string message;
		public string notes;
		public string author;
	}
}
