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
using US13.Managers.AddressableManager;

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
		private int loadVersion;
		private AddressableCatalogueManager subscribedManager;
		public List<GameObject> audioButtons = new List<GameObject>();

		public static HashSet<string> audioList = new HashSet<string>();

		protected virtual void Awake()
		{
			SearchBar = GetComponentInChildren<AdminGlobalAudioSearchBar>();
		}


		protected virtual void OnEnable()
		{
			ReloadButtons();
		}

		protected virtual void OnDisable()
		{
			loadVersion++;
			if (subscribedManager != null) subscribedManager.AudioRegistryChanged -= ReloadButtons;
			subscribedManager = null;
		}

		private void ReloadButtons()
		{
			foreach (var button in audioButtons)
				if (button != null)
					Destroy(button);
			audioButtons.Clear();
			LoadButtons();
		}

		public static void DoLoadAudio()
		{
			audioList = new HashSet<string>(AddressableCatalogueManager.Instance?.AudioAddresses ??
			                                Enumerable.Empty<string>());
		}

		/// <summary>
		/// Generates buttons for the audio list
		/// </summary>
		private async void LoadButtons()
		{
			int version = ++loadVersion;
			loadingView.SetActive(true);
			try
			{
				// The window can be enabled before the initialisation queue reaches Addressables.
				float deadline = Time.realtimeSinceStartup + 10f;
				while (AddressableCatalogueManager.Instance == null)
				{
					if (this == null || version != loadVersion) return;
					if (Time.realtimeSinceStartup >= deadline)
						throw new System.InvalidOperationException(
							"AddressableCatalogueManager was not initialized. Check the Managers prefab's startup systems.");
					await UniTask.NextFrame();
				}

				if (this == null || version != loadVersion) return;
				var manager = AddressableCatalogueManager.Instance;
				if (subscribedManager != manager)
				{
					if (subscribedManager != null) subscribedManager.AudioRegistryChanged -= ReloadButtons;
					subscribedManager = manager;
					manager.AudioRegistryChanged += ReloadButtons;
				}

				await manager.WaitForInitialLoadAsync();
				if (this == null || version != loadVersion) return;
				DoLoadAudio();
				if (audioList.Count == 0)
					throw new System.InvalidOperationException(
						$"No audio addresses were loaded. Catalogue error: {manager.LastLoadError ?? "none"}. Check LobbyAddressableCatalogues and the base SoundAndMusic build.");

				if (SearchBar != null)
				{
					SearchBar.Resettext();
				}

				var addresses = audioList.OrderBy(address => address, System.StringComparer.Ordinal).ToList();
				int index = 0;
				foreach (string address in addresses)
				{
					if (this == null || version != loadVersion) return;
					// Show usable entries as they load rather than waiting for the entire catalogue.
					var audio = await AudioManager.GetAddressableAudioSourceFromCache(new AddressableAudioSource(address));
					await UniTask.SwitchToMainThread();
					if (this == null || version != loadVersion) return;
					loadingBar.size = (float)++index / addresses.Count;
					if (index % 16 == 0) await UniTask.NextFrame();
					if (this == null || version != loadVersion) return;
					if (audio == null) continue;
					AudioSource source = audio.AudioSource;
					if (source == null || source.clip == null || source.loop) continue;
					GameObject button = Instantiate(buttonTemplate); //creates new button
					button.SetActive(true);
					AdminGlobalAudioButton buttonScript = button.GetComponent<AdminGlobalAudioButton>();
					buttonScript.SetText($"{source.clip.name}\n {(int)source.clip.length} seconds");
					buttonScript.SoundAddress = audio.AssetAddress;
					audioButtons.Add(button);
					button.transform.SetParent(buttonTemplate.transform.parent, false);
					loadingView.SetActive(false);
				}
			}
			catch (System.Exception exception)
			{
				Loggy.Error($"Admin audio list failed: {exception}", Category.Audio);
			}
			finally
			{
				if (this != null && version == loadVersion) loadingView.SetActive(false);
			}
		}

		public virtual void PlayAudio(string index)
		{
		}
	}
}
