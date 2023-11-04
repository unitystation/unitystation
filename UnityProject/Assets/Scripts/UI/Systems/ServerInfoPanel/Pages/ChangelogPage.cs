using System.Collections;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Logs;
using Newtonsoft.Json;
using SecureStuff;
using UI.Systems.ServerInfoPanel.Models;
using UnityEngine;
using UnityEngine.UI;
using Util.Rx;

namespace UI.Systems.ServerInfoPanel
{
	public class ChangelogPage: InfoPanelPage
	{
		[SerializeField] private Button previousButton;
		[SerializeField] private Button nextButton;
		[SerializeField] private GameObject buildEntryPrefab;
		[SerializeField] private Transform changesContainer;

		private const string BASE_API_URL = "https://changelog.unitystation.org/all-changes";

		private readonly BehaviourSubject<AllChangesResponse> changelogData = new(null);
		private const int CHUNK_SIZE = 10;


		public void RefreshPage()
		{
			DestroyAllEntries();
			RefreshPaginationButtons();
			if (changelogData.Value == null) return;


			StartCoroutine(SpawnPrefabs());
		}

		private IEnumerator SpawnPrefabs()
		{
			if (changelogData.Value == null) yield break;

			//Separate the entries into chunks of 10
			foreach (var chunk in changelogData.Value.results.Chunk(CHUNK_SIZE))
			{
				foreach (var build in chunk)
				{
					var buildEntry = Instantiate(buildEntryPrefab, changesContainer);
					buildEntry.GetComponent<BuildEntry>().SetBuild(build);
				}

				//wait for next frame to keep spawning
				yield return null;
			}
		}

		private void DestroyAllEntries()
		{
			foreach (Transform child in changesContainer)
			{
				Destroy(child.gameObject);
			}
		}


		private void RefreshPaginationButtons()
		{
			previousButton.interactable = changelogData?.Value?.previous != null;
			nextButton.interactable = changelogData?.Value?.next != null;
		}

		public override bool HasContent()
		{
			return true;
		}

		private void ChangeDataPage(string newPage)
		{
			if (string.IsNullOrEmpty(newPage)) return;
			FetchChanges(newPage).Then(
				(newData) => { changelogData.Next(newData.Result); }
			);
		}

		public void OnNextButtonClicked()
		{
			ChangeDataPage(changelogData.Value.next);
		}

		public void OnPreviousButtonClicked()
		{
			ChangeDataPage(changelogData.Value.previous);
		}

		private static async Task<AllChangesResponse> FetchChanges(string url = BASE_API_URL)
		{
			AllChangesResponse newData = null;

			// Create an instance of HttpRequestMessage
			HttpRequestMessage request = new HttpRequestMessage( HttpMethod.Get,url );
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));



			using var response = await SafeHttpRequest.SendAsync(request);
			if (response.IsSuccessStatusCode)
			{
				var json = await response.Content.ReadAsStringAsync();
				newData = JsonConvert.DeserializeObject<AllChangesResponse>(json);
			}
			else
			{
				Loggy.LogError($"Failed to fetch changelog from {url}", Category.UI);
				Loggy.LogError($"Status: {response.StatusCode}. Reason: {response.ReasonPhrase}", Category.UI);
			}

			return newData;
		}

		#region Lifecycle
		private void Awake()
		{
			changelogData.Subscribe(
				_ => RefreshPage()
			);
		}

		private void OnDestroy()
		{
			changelogData.Unsubscribe(
				_ => RefreshPage()
			);
		}

		private void OnEnable()
		{
			FetchChanges().Then(
				(newData) => { changelogData.Next(newData.Result); }
			);
		}
		#endregion
	}
}