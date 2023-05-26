using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Audio.Containers;
using AddressableReferences;
using Newtonsoft.Json.Linq;
using UnityEditor;
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

		[SerializeField] private AudioClipsArray audioAddressables = null;

		public Dictionary<AddressableAudioSource, string> audioList = new Dictionary<AddressableAudioSource, string>();
		public CatalogueData catalogueData;

		private void Awake()
		{
			SearchBar = GetComponentInChildren<AdminGlobalAudioSearchBar>();
#if UNITY_EDITOR
			catalogueData = AssetDatabase.LoadAssetAtPath<CatalogueData>("Assets/CachedData/CatalogueData.asset");
#endif

			Refresh();
			DoLoadAudio(catalogueData);
		}

		public static List<string> GetCataloguePath()
		{
			var path = Application.dataPath.Remove(Application.dataPath.IndexOf("/Assets"));
			path += "/AddressablePackingProjects";
			Logger.Log(path, Category.Addressables);
			var Directories = Directory.GetDirectories(path);
			var FoundFiles = new List<string>();
			foreach (var Directori in Directories)
			{
				var newpath = Directori + "/ServerData";
				if (Directory.Exists(newpath))
				{
					var Files = Directory.GetFiles(newpath);

					string FoundFile = "";
					foreach (var File in Files)
					{
						//Logger.Log(File);
						if (File.EndsWith(".json"))
						{
							if (FoundFile != "")
							{
								Logger.LogError("two catalogues present please only ensure one", Category.Addressables);
							}

							FoundFile = File;
						}
					}

					if (FoundFile == "")
					{
						Logger.LogWarning("missing json file", Category.Addressables);
					}
					else
					{
						FoundFiles.Add(FoundFile);
					}
				}
			}

			return FoundFiles;
		}

		public void Refresh()
		{
			var FoundFiles = GetCataloguePath();
			foreach (var FoundFile in FoundFiles)
			{
				JObject o1 = JObject.Parse(File.ReadAllText((@FoundFile.Replace("/", @"/"))));
				var IDs = o1.GetValue("m_InternalIds");
				var ListIDs = IDs.ToObject<List<string>>().Where(x => x.Contains(".bundle") == false);

				if (catalogueData == null)
				{
					Logger.LogError("Couldn't find catalogue data!");
				}
				var flip = new FileInfo(FoundFile);
				var ToPutInList = ListIDs.ToList();
				ToPutInList.Insert(0, "null");
				catalogueData.Data[flip.Directory.Parent.Name] = ToPutInList;
			}
		}

		private async void DoLoadAudio(CatalogueData audioAddressables)
		{
			loadingView.SetActive(true);
			audioList = new Dictionary<AddressableAudioSource, string>();

			loadingBar.size = 0;

			foreach (var audioSources in audioAddressables.Data.Values)
			{
				var count = 0;
				foreach (var audioSource in audioSources)
				{
					loadingBar.size = (count - 0.1f) / (audioSources.Count() - 0.1f);
					AddressableAudioSource audio = new AddressableAudioSource();
					audio.AssetAddress = audioSource;
					audio = await AudioManager.GetAddressableAudioSourceFromCache(audio);
					if(audio != null) audioList.Add(audio, audioSource);
					count++;
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
