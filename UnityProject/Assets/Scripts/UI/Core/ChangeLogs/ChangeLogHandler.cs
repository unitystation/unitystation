using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace UI
{
	public class ChangeLogHandler : MonoBehaviour
	{
		public GameObject window;
		public Transform content;
		private List<ChangeLogEntry> allEntries = new List<ChangeLogEntry>();
		public GameObject entryPrefab;
		public string ChangeLogLink = "https://changelog.unitystation.org/all-changes?format=json&limit=50";
		public string ChangeLogParams = "all-changes?format=json&limit=50";

		void Start()
		{
			if (string.IsNullOrEmpty(ChangeLogLink))
			{
				window.SetActive(false);
				return;
			}
			GrabJsonData();
		}

		[Button("Get Data")]
		private void GrabJsonData()
		{
			var client = new HttpClient();
			client.BaseAddress = new Uri(ChangeLogLink);
			// Add an Accept header for JSON format.
			client.DefaultRequestHeaders.Accept.Add(
				new MediaTypeWithQualityHeaderValue("application/json"));
			// Get data response
			var response = client.GetAsync(ChangeLogParams).Result;
			response.EnsureSuccessStatusCode();

			//if we failed getting the data
			if (response.IsSuccessStatusCode == false)
			{
				Logger.LogError(response.ReasonPhrase);
				window.SetActive(false);
				response.Dispose();
				client.Dispose();
				return;
			}
			//ReadAsAsync<>() has been deprecated long ago so use ReadAsStringAsync() instead
			string responseBody = response.Content.ReadAsStringAsync().Result;
			//Read the data from the json then add it to the entries list
			StringReader dataString = new StringReader(responseBody);
			allEntries.AddRange(ReadFromJson(dataString).results);
			//avoid memory leaks by disposing all of this
			dataString.Dispose();
			response.Dispose();
			client.Dispose();

			//Show the changes
			OpenWindow();
		}

		private ChangeLogJson ReadFromJson(StringReader dataString)
		{
			//JsonConvert causes heavy memoryleaks and JsonUtilites doesn't work, use JsonSerializer instead.
			//Yes its slightly ugly but do you want your PC to bluescreen or have clean code that doesn't function?
			JsonSerializer serializer = JsonSerializer.Create();
			JsonReader reader = new JsonTextReader(dataString);
			return serializer.Deserialize<ChangeLogJson>(reader);
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
	public class ChangeLogJson
	{
		public int count { get; set; }
		[CanBeNull] public string next { get; set; }
		[CanBeNull] public string previous { get; set; }
		public List<ChangeLogEntry> results { get; set; }
	}

	[Serializable]
	public class ChangeLogEntry
	{
		public string author_username { get; set; }
		public string author_url { get; set; }
		public string description { get; set; }
		public string pr_url { get; set; }
		public int pr_number { get; set; }
		public string category { get; set; }
		public string build { get; set; }
		public string date_added { get; set; }
	}
}
