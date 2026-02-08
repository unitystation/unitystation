using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Logs;
using SecureStuff;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using US13.Core.Addressables.Types;
using US13.Core.Database;
using US13.Managers;

namespace US13.UI.Systems.AdminTools
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

		public static HashSet<string> audioList = new HashSet<string>();

		private void Awake()
		{
			SearchBar = GetComponentInChildren<AdminGlobalAudioSearchBar>();
			LoadButtons();
		}


		public static async void DoLoadAudio()
		{

			//loadingView.SetActive(true);
			audioList = new HashSet<string>();
			var serverCatalouges = new List<string>();
			serverCatalouges.AddRange(ServerData.ServerConfig.AddressableCatalogues);
			serverCatalouges.AddRange(ServerData.ServerConfig.LobbyAddressableCatalogues);
			//loadingBar.size = 0;
			//(Max): This shit wont work correctly in the editor when adding new sounds but i don't give a fuck anymore.
			//Fuck addressables and I hope everyone who agreed to use addressables in this project to be forced into an
			//ALS Ice Bucket Challenge, CIA style.
			foreach (var serverCatalouge in serverCatalouges.Where(serverCatalouge => serverCatalouge != string.Empty))
			{
				Loggy.Info(serverCatalouge);
				AsyncOperationHandle<IResourceLocator> task;
				if (serverCatalouge.Contains("http"))
				{
					string result = await SafeHttpRequest.GetStringAsync(serverCatalouge);
					Loggy.Info(result);
					task = Addressables.LoadContentCatalogAsync(result);
					await task.Task;
				}
				else
				{
					task = Addressables.LoadContentCatalogAsync(serverCatalouge);
					await task.Task;
				}

				var count = 0;
				foreach (var audioSources in task.Result.Keys)
				{
					//loadingBar.size = (count - 0.1f) / (task.Result.Keys.Count() - 0.1f);
					count++;
					if (audioSources.ToString().Contains("/") == false) continue;
					try
					{
						audioList.Add(audioSources.ToString());
					}
					catch
					{
						continue;
					}
				}
			}
			//loadingView.SetActive(false);

		}

		/// <summary>
		/// Generates buttons for the audio list
		/// </summary>
		private async void LoadButtons()
		{
			loadingView.SetActive(true);

			if (SearchBar != null)
			{
				SearchBar.Resettext();
			}

			List<Task<AddressableAudioSource>> audioSources = new List<Task<AddressableAudioSource>>();

			int index = 0;
			foreach (var audio in audioList)
			{
				AddressableAudioSource audioSource = new AddressableAudioSource();
				audioSource.AssetAddress = audio.ToString();
				audioSources.Add(AudioManager.GetAddressableAudioSourceFromCache(audioSource));
			}

			var results = await Task.WhenAll(audioSources);
			await UniTask.SwitchToMainThread();
			foreach (var audio in results)
			{
				if (audio == null) continue;
				loadingBar.size = (index - 0.1f) / (audioList.Count() - 0.1f);
				AudioSource source = audio.AudioSource;
				if (source.loop) continue;
				GameObject button = Instantiate(buttonTemplate); //creates new button
				button.SetActive(true);
				AdminGlobalAudioButton buttonScript = button.GetComponent<AdminGlobalAudioButton>();
				buttonScript.SetText($"{source.clip.name}\n {(int)source.clip.length} seconds");
				buttonScript.SoundAddress = audio.AssetAddress;
				audioButtons.Add(button);
				button.transform.SetParent(buttonTemplate.transform.parent, false);
			}

			loadingView.SetActive(false);
		}

		public virtual void PlayAudio(string index) {}
	}
}
