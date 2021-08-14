using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using DatabaseAPI;
using Initialisation;
using Messages.Client.Addressable;
using Messages.Server.Addressable;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;

public class AddressableCatalogueManager : MonoBehaviour, IInitialise
{
	//TODO
	//Cleanup errors
	//think about builds and versioning
	public AddressableDownloadPopUp AddressableDownloadPopUp;


	public static AddressableCatalogueManager Instance;

	public InitialisationSystems Subsystem => InitialisationSystems.Addressables;

	void IInitialise.Initialise()
	{
		Instance = this;
		FinishLoaded = false;
		Addressables.InitializeAsync();

		var cool = new List<string>();
		if (Application.isEditor)
		{
			cool.AddRange(GetCataloguePath());
		}
		else if (GameData.Instance.DevBuild)
		{
			cool.AddRange(GetCataloguePathStreamingAssets());
		}
		else
		{
			cool.AddRange(ServerData.ServerConfig.LobbyAddressableCatalogues);
			LoadCatalogue(cool, false);
			return;
		}
		LoadCatalogue(cool);
	}


	public int ToloadeCount = 0;
	public int CompletedLoad = 0;

	public class LoadCounter

	{
		public int GetTo;
		public int Current;
		public Action onComplete;

		public LoadCounter(int _GetTo, Action _onComplete)
		{
			GetTo = _GetTo;
			Current = 0;
			onComplete = _onComplete;
		}

		public void IncrementAndCheckLoad()
		{
			Current++;
			if (Current >= GetTo)
			{
				onComplete.Invoke();
			}
		}
	}

	public static bool FinishLoaded = false;

	private static async void LoadCatalogue(List<string> LoadCatalogues, bool RegisterComplete = true)
	{
		Instance.ToloadeCount = LoadCatalogues.Count;
		foreach (var Catalogue in LoadCatalogues)
		{

			if (Catalogue.Contains("http"))
			{
				HttpClient client = new HttpClient();
				string result = await client.GetStringAsync(Catalogue);
				var Task = Addressables.LoadContentCatalogAsync(result);
				await Task.Task;
				Instance.AssetBundleDownloadDependencies(Task, RegisterComplete);
			}
			else
			{
				var Task = Addressables.LoadContentCatalogAsync(Catalogue);
				await Task.Task;
				Instance.AssetBundleDownloadDependencies(Task, RegisterComplete);
			}

		}
	}

	public void AssetBundleDownloadDependencies(AsyncOperationHandle<IResourceLocator> Content, bool RegisterComplete =
		true)
	{
		ResourceLocationMap locMap = Content.Result as ResourceLocationMap;

		HashSet<IResourceLocation> Tolode = new HashSet<IResourceLocation>();

		HashSet<IResourceLocation> FoundDependencies = new HashSet<IResourceLocation>();
		foreach (var Locator in locMap.Locations)
		{
			foreach (var Resource in Locator.Value)
			{
				if (Resource.HasDependencies)
				{
					foreach (var Dependencie in Resource.Dependencies)
					{
						if (FoundDependencies.Contains(Dependencie) == false) //Check if it's a new dependency
						{
							FoundDependencies.Add(Dependencie);
							Tolode.Add(
								Resource); //Then add this to the Load dependency thing idk why can't you Load the dependency directly
						}
					}
				}
			}
		}

		if (RegisterComplete)
		{
			var ContentLoadTracker = new LoadCounter(Tolode.Count, RegisterLoad);
			AddressableDownloadPopUp.DownloadDependencies(Tolode.ToList(), ContentLoadTracker);
		}

		else
		{
			AddressableDownloadPopUp.DownloadDependencies(Tolode.ToList());
		}
	}


	private void RegisterLoad()
	{
		CompletedLoad++;
		if (ToloadeCount == CompletedLoad)
		{
			FinishLoaded = true;
		}
	}

	public static void LoadHostCatalogues()
	{
		if (Application.isEditor) return;
		if (GameData.Instance.DevBuild)
		{
			LoadCatalogue(GetCataloguePathStreamingAssets());
		}
		else
		{
			LoadCatalogue(ServerData.ServerConfig.AddressableCatalogues);
		}
	}

	[Server]
	public static void ClientRequestCatalogue(GameObject PlayerGameObject)
	{
//TODO Need spam Protection
		SendCataloguesToClient.Send(ServerData.ServerConfig.AddressableCatalogues, PlayerGameObject);
	}

	public IEnumerator WaitForLoad()
	{
		yield return WaitFor.Seconds(5f);
		ClientRequestCatalogues.RequestCatalogue();
	}

	public void LoadClientCatalogues()
	{
		this.gameObject.SetActive(true);
		StartCoroutine(WaitForLoad());
	}

	public static void LoadCataloguesFromServer(List<string> toLoad)
	{
//Can add some checks here
		LoadCatalogue(toLoad);
	}

	public static List<string> GetCataloguePath()
	{
		var path = Application.dataPath.Remove(Application.dataPath.IndexOf("/Assets"));
		//path = path + "/AddressablePackingProjects/SoundAndMusic/ServerData"; //Make OS agnostic
		path = path + "/AddressablePackingProjects";
		//Logger.Log(path);
		var Directories = System.IO.Directory.GetDirectories(path);
		var FoundFiles = new List<string>();
		foreach (var Directori in Directories)
		{
			var newpath = Directori + "/ServerData";
			if (System.IO.Directory.Exists(newpath))
			{
				var Files = System.IO.Directory.GetFiles(newpath);

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


	public static List<string> GetCataloguePathStreamingAssets()
	{
		var pathss = Application.streamingAssetsPath + "/AddressableCatalogues";
		var Directories = System.IO.Directory.GetDirectories(pathss);
		var Catalogues = new List<string>();
		foreach (var Directorie in Directories)
		{
			var newpaths = Directorie.Replace(@"\", "/");
			var newDirectories = System.IO.Directory.GetFiles(newpaths);

			foreach (var pathST in newDirectories)
			{
				if (pathST.Contains(".json"))
				{
					Catalogues.Add(pathST);
				}

			}
		}

		return Catalogues;
	}
}