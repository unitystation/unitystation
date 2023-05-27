using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using UnityEngine;
using Audio.Containers;
using AddressableReferences;
using DatabaseAPI;
using GameConfig;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace AdminTools
{
	/// <summary>
	/// Lets Admins play Audio
	/// </summary>
	public class AdminGlobalAudio : MonoBehaviour
	{
		[SerializeField] private GameObject buttonTemplate = null;
		[SerializeField] private Scrollbar loadingBar = null;
		[SerializeField] private GameObject loadingView = null;
		private AdminGlobalAudioSearchBar SearchBar;
		public List<GameObject> audioButtons = new List<GameObject>();

		public Dictionary<AddressableAudioSource, string> audioList = new Dictionary<AddressableAudioSource, string>();

		private void Awake()
		{
			SearchBar = GetComponentInChildren<AdminGlobalAudioSearchBar>();
			DoLoadAudio();
		}

		private async void DoLoadAudio()
		{
			loadingView.SetActive(true);
			audioList = new Dictionary<AddressableAudioSource, string>();
			var serverCatalouges = new List<string>();
			serverCatalouges.AddRange(ServerData.ServerConfig.AddressableCatalogues);
			serverCatalouges.AddRange(ServerData.ServerConfig.LobbyAddressableCatalogues);
			loadingBar.size = 0;
			//(Max): This shit wont work correctly in the editor when adding new sounds but i don't give a fuck anymore.
			//Fuck addressables and I hope everyone who agreed to use addressables in this project to be forced into an
			//ALS Ice Bucket Challenge, CIA style.
			foreach (var serverCatalouge in serverCatalouges.Where(serverCatalouge => serverCatalouge != string.Empty))
			{
				Logger.Log(serverCatalouge);
				var Task = new AsyncOperationHandle<IResourceLocator>();
				if (serverCatalouge.Contains("http"))
				{
					HttpClient client = new HttpClient();
					string result = await client.GetStringAsync(serverCatalouge);
					Logger.Log(result);
					Task = Addressables.LoadContentCatalogAsync(result);
					await Task.Task;
				}
				else
				{
					Task = Addressables.LoadContentCatalogAsync(serverCatalouge);
					await Task.Task;
				}

				var count = 0;
				foreach (var audioSources in Task.Result.Keys)
				{
					loadingBar.size = (count - 0.1f) / (Task.Result.Keys.Count() - 0.1f);
					count++;
					try
					{
						AddressableAudioSource audio = new AddressableAudioSource();
						audio.AssetAddress = audioSources.ToString();
						audio = await AudioManager.GetAddressableAudioSourceFromCache(audio);
						if(audio != null) audioList.Add(audio, audioSources.ToString());
					}
					catch
					{
						continue;
					}
				}
			}
			loadingView.SetActive(false);
			LoadButtons();
		}

		/// <summary>
		/// Generates buttons for the audio list
		/// </summary>
		private void LoadButtons()
		{
			if (SearchBar != null)
			{
				SearchBar.Resettext();
			}

			int index = 0;
			foreach (var audio in audioList)
			{
				AudioSource source = audio.Key.AudioSource;
				if (!source.loop)
				{
					GameObject button = Instantiate(buttonTemplate) as GameObject; //creates new button
					button.SetActive(true);
					AdminGlobalAudioButton buttonScript = button.GetComponent<AdminGlobalAudioButton>();
					buttonScript.SetText($"{source.clip.name}\n {(int)source.clip.length} seconds");
					buttonScript.SoundAddress = audio.Value;
					audioButtons.Add(button);
					button.transform.SetParent(buttonTemplate.transform.parent, false);
				}
			}
		}

		public virtual void PlayAudio(string index) {}
	}
}
