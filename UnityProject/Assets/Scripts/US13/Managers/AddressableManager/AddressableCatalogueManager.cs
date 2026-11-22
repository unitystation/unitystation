using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Logs;
using Mirror;
using SecureStuff;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using US13.Core.Database;
using US13.Core.Initialisation;
using US13.Messages.Client.Addressable;
using US13.Messages.Server.Addressable;
using US13.UI.Systems.AdminTools;

namespace US13.Managers.AddressableManager
{
	/// <summary>Data-only server audio configuration. Addresses must be unique across packs.</summary>
	[Serializable]
	public class ServerAudioManifest
	{
		public int Revision;
		public string Platform;
		public List<string> Catalogues = new List<string>();
		public Dictionary<string, List<string>> PlatformCatalogues = new Dictionary<string, List<string>>();
		public Dictionary<string, string> Overrides = new Dictionary<string, string>();
		public List<string> LobbyTracks = new List<string>();
		public bool ReplaceLobbyPlaylist;
	}

	public class AddressableCatalogueManager : MonoBehaviour, IInitialise
	{
		public const string LocalTestCataloguePrefix = "streaming-assets://AudioModdingTest/";
		public AddressableDownloadPopUp AddressableDownloadPopUp;
		public static AddressableCatalogueManager Instance;
		public InitialisationSystems Subsystem => InitialisationSystems.Addressables;
		public static bool FinishLoaded;
		public string LastLoadError { get; private set; }
		public int ToloadeCount;
		public int CompletedLoad;
		public event Action AudioRegistryChanged;
		private readonly SemaphoreSlim loadLock = new SemaphoreSlim(1, 1);
		private readonly Dictionary<string, IResourceLocator> baseline = new Dictionary<string, IResourceLocator>();
		private readonly Dictionary<string, IResourceLocator> session = new Dictionary<string, IResourceLocator>();
		private Dictionary<string, string> overrides = new Dictionary<string, string>();
		private int generation;
		private bool awaitingServerManifest;
		private Task initialLoad = Task.CompletedTask;
		private bool initialisationStarted;
		private readonly TaskCompletionSource<bool> startupReady = new TaskCompletionSource<bool>();
		private IResourceLocator builtInLocator;
		private Action<UnityEngine.Networking.UnityWebRequest> previousRequestOverride;
		private Action<UnityEngine.Networking.UnityWebRequest> requestOverride;

		private void Awake()
		{
			// ThemeManager preloads audio before this system's queued Initialise call.
			// Publish a pending readiness task as soon as scene objects are awake.
			Instance = this;
		}

		void IInitialise.Initialise()
		{
			EnsureInitialised();
		}

		public static AddressableCatalogueManager FindInstance()
		{
			// Unity objects can be destroyed while their managed/static reference survives.
			if (Instance == null) Instance = FindFirstObjectByType<AddressableCatalogueManager>(FindObjectsInactive.Include);
			return Instance;
		}

		public void EnsureInitialised()
		{
			if (initialisationStarted) return;
			initialisationStarted = true;
			Instance = this;
			previousRequestOverride = Addressables.WebRequestOverride;
			requestOverride = request =>
			{
				previousRequestOverride?.Invoke(request);
				if (request.timeout == 0 || request.timeout > 30) request.timeout = 30;
			};
			Addressables.WebRequestOverride = requestOverride;
			FinishLoaded = false;
			initialLoad = LoadInitialAsync();
		}

		private void OnDestroy()
		{
			generation++;
			startupReady.TrySetCanceled();
			if (requestOverride != null && Addressables.WebRequestOverride == requestOverride)
				Addressables.WebRequestOverride = previousRequestOverride;
			if (Instance == this) Instance = null;
		}

		private async Task LoadInitialAsync()
		{
			try
			{
				var initialization = Addressables.InitializeAsync(false);
				try
				{
					await initialization.Task;
					if (initialization.Status != AsyncOperationStatus.Succeeded)
						throw initialization.OperationException;
					// Fast Play Mode returns an AssetDatabase-backed settings locator. It is not a
					// built content catalogue and can throw while enumerating missing editor assets.
					builtInLocator = initialization.Result as ResourceLocationMap;
				}
				finally
				{
					if (initialization.IsValid()) Addressables.Release(initialization);
				}

				var paths = new List<string>();
				if (Application.isEditor)
				{
#if UNITY_EDITOR
					paths.AddRange(GetCataloguePath());
#endif
				}
				else if (GameData.Instance.DevBuild) paths.AddRange(GetCataloguePathStreamingAssets());

				bool baseLoaded = paths.Count > 0 && await LoadAsync(new ServerAudioManifest { Catalogues = paths }, false, generation);
				if (!baseLoaded)
				{
					FinishLoaded = false;
					paths = GetStartupCatalogues(ServerData.ServerConfig);
					await LoadAsync(new ServerAudioManifest { Catalogues = paths }, false, generation);
				}
				if (Application.isEditor && ServerData.ServerConfig.EnableLocalAudioTest && ServerData.ServerConfig.AudioManifest != null)
					await LoadAsync(ServerData.ServerConfig.AudioManifest, true, generation);
			}
			catch (Exception exception)
			{
				ReportServerManifestError(exception.Message);
				Loggy.Error($"Audio startup failed: {exception}", Category.Addressables);
			}
			finally { startupReady.TrySetResult(true); }
		}

		public static List<string> GetStartupCatalogues(ServerConfig config)
		{
			if (config == null)
				throw new InvalidOperationException("Server configuration is not loaded before audio startup.");
			var paths = config.LobbyAddressableCatalogues?.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct()
				.ToList();
			return paths != null && paths.Count > 0
				? paths
				: (config.AddressableCatalogues ?? new List<string>()).Where(path => !string.IsNullOrWhiteSpace(path))
				.Distinct().ToList();
		}

		public Task WaitForInitialLoadAsync() => startupReady.Task;

		public static ServerAudioManifest GetServerManifest()
		{
			var configured = ServerData.ServerConfig.AudioManifest;
			return new ServerAudioManifest
			{
				Revision = configured?.Revision ?? 0,
				Platform = configured?.Platform,
				PlatformCatalogues = configured?.PlatformCatalogues ?? new Dictionary<string, List<string>>(),
				Catalogues = (ServerData.ServerConfig.AddressableCatalogues ?? new List<string>())
					.Concat(configured?.Catalogues ?? new List<string>()).Distinct().ToList(),
				Overrides = configured?.Overrides ?? new Dictionary<string, string>(),
				LobbyTracks = configured?.LobbyTracks ?? new List<string>(),
				ReplaceLobbyPlaylist = configured?.ReplaceLobbyPlaylist ?? false
			};
		}

		public static void LoadHostCatalogues()
		{
			var manager = FindInstance();
			if (manager != null) _ = manager.ApplyServerManifestAsync(GetServerManifest());
			else Loggy.Error("Cannot load host audio: AddressableCatalogueManager is missing from the scene.", Category.Addressables);
		}

		[Server]
		public static void ClientRequestCatalogue(GameObject player)
		{
			SendCataloguesToClient.SendManifest(GetServerManifest(), player);
		}

		public void LoadClientCatalogues()
		{
			awaitingServerManifest = true;
			FinishLoaded = false;
			gameObject.SetActive(true);
		}

		public static void LoadCataloguesFromServer(List<string> paths)
		{
			var manager = FindInstance();
			if (manager == null) throw new InvalidOperationException("Audio catalogue manager is missing from the scene.");
			_ = manager.ApplyServerManifestAsync(new ServerAudioManifest { Catalogues = paths });
		}

		public async Task<bool> ApplyServerManifestAsync(ServerAudioManifest manifest)
		{
			awaitingServerManifest = true;
			FinishLoaded = false;
			EnsureInitialised();
			int requestGeneration = generation;
			try { await WaitForInitialLoadAsync(); }
			catch (OperationCanceledException) { return false; }
			return await LoadAsync(manifest, true, requestGeneration);
		}

		public void ReportServerManifestError(string error)
		{
			LastLoadError = error;
			awaitingServerManifest = false;
			FinishLoaded = true;
		}

		private async Task<bool> LoadAsync(ServerAudioManifest manifest, bool serverSession, int requestGeneration)
		{
			await loadLock.WaitAsync();
			var staged = new Dictionary<string, IResourceLocator>();
			try
			{
				if (serverSession && requestGeneration != generation) return false;
				LastLoadError = null;
				ValidateManifest(manifest);
				if (!string.IsNullOrEmpty(manifest.Platform) && manifest.Platform != Application.platform.ToString())
					throw new ArgumentException("Audio pack platform does not match this client.");
				var paths = manifest.Catalogues.Distinct().ToList();
				if (manifest.PlatformCatalogues.TryGetValue(Application.platform.ToString(), out var platformPaths))
					paths.AddRange(platformPaths);
				paths = paths.Distinct().ToList();
				if (serverSession && baseline.Count > 0 && NetworkServer.active &&
				    (Application.isEditor || GameData.Instance.DevBuild))
					paths.RemoveAll(path => ServerData.ServerConfig.LobbyAddressableCatalogues?.Contains(path) == true);
				ToloadeCount = paths.Count;
				CompletedLoad = 0;
				foreach (string path in paths)
				{
					if (serverSession && requestGeneration != generation) return false;
					string url = await ResolveCatalogueAsync(path, serverSession);
					Loggy.Info($"Loading audio catalogue: {url}", Category.Addressables);
					if (serverSession && requestGeneration != generation) return false;
					if (baseline.TryGetValue(url, out var existing) || session.TryGetValue(url, out existing))
					{
						staged[url] = existing;
					}
					else
					{
						var handle = Addressables.LoadContentCatalogAsync(url, false);
						try
						{
							await handle.Task;
							if (handle.Status != AsyncOperationStatus.Succeeded) throw handle.OperationException;
							staged[url] = handle.Result;
							// Do not expose partially downloaded catalogues to playback.
							Addressables.RemoveResourceLocator(handle.Result);
						}
						finally
						{
							if (handle.IsValid()) Addressables.Release(handle);
						}
					}

					if (serverSession && requestGeneration != generation) return false;
					var locations = GetLocations(staged[url]);
					if (locations.Count == 0)
						throw new InvalidOperationException($"Audio catalogue contains no GameObject locations: {url}.");
					await ValidateRemoteLocationsAsync(locations, serverSession && !path.StartsWith(LocalTestCataloguePrefix, StringComparison.Ordinal));
					if (serverSession && requestGeneration != generation) return false;
					if (locations.Count > 0)
					{
						var download = Addressables.DownloadDependenciesAsync(locations, false);
						// The bar owns no handle; the loader always releases it, including failures.
						var bar = AddressableDownloadPopUp == null
							? null
							: AddressableDownloadPopUp.Track(download, path);
						try
						{
							await download.Task;
							if (download.Status != AsyncOperationStatus.Succeeded) throw download.OperationException;
						}
						finally
						{
							if (bar != null) Destroy(bar.gameObject);
							if (download.IsValid()) Addressables.Release(download);
						}
					}

					CompletedLoad++;
				}

				if (serverSession && requestGeneration != generation) return false;
				var available = baseline.Values.Concat(staged.Values).Distinct().ToList();
				foreach (string address in manifest.Overrides.Values.Concat(manifest.LobbyTracks))
				{
					var location = ResolveLocation(address, available);
					var validation = Addressables.LoadAssetAsync<GameObject>(location);
					try
					{
						var prefab = await validation.Task;
						if (prefab == null || !prefab.TryGetComponent<AudioSource>(out var source) ||
						    source.clip == null)
							throw new ArgumentException($"Audio prefab has no AudioSource/clip: {address}");
					}
					finally
					{
						if (validation.IsValid()) Addressables.Release(validation);
					}
				}

				if (serverSession && requestGeneration != generation) return false;
				if (serverSession)
				{
					LobbyManager.MusicManager.Instance?.SetServerPlaylist(null, false);
					AudioManager.ClearAudioCache();
					foreach (var locator in session.Values.Except(baseline.Values))
						Addressables.RemoveResourceLocator(locator);
					session.Clear();
					overrides = new Dictionary<string, string>(manifest.Overrides);
				}

				var target = serverSession ? session : baseline;
				foreach (var pair in staged)
				{
					target[pair.Key] = pair.Value;
					if (!Addressables.ResourceLocators.Contains(pair.Value))
						Addressables.AddResourceLocator(pair.Value);
				}

				if (serverSession)
					LobbyManager.MusicManager.Instance?.SetServerPlaylist(manifest.LobbyTracks,
						manifest.ReplaceLobbyPlaylist);
				if (serverSession) awaitingServerManifest = false;
				FinishLoaded = !awaitingServerManifest;
				Loggy.Info($"Audio catalogues ready: {CompletedLoad}/{ToloadeCount}", Category.Addressables);
				NotifyAudioRegistryChanged();
				return true;
			}
			catch (Exception exception)
			{
				LastLoadError = exception.Message;
				Loggy.Error($"Audio content load failed: {exception}", Category.Addressables);
				// Optional server audio must not strand scene loading. Previous registry remains active.
				if (!serverSession || requestGeneration == generation)
				{
					if (serverSession) awaitingServerManifest = false;
					FinishLoaded = !awaitingServerManifest;
				}

				return false;
			}
			finally
			{
				loadLock.Release();
			}
		}

		public static void ValidateManifest(ServerAudioManifest manifest)
		{
			if (manifest == null) throw new ArgumentNullException(nameof(manifest));
			if (manifest.PlatformCatalogues == null || manifest.PlatformCatalogues.Count > 16 ||
			    manifest.PlatformCatalogues.Values.Any(value => value == null || value.Count > 32))
				throw new ArgumentException("Invalid platform audio catalogues.");
			if (manifest.Catalogues == null || manifest.Overrides == null || manifest.LobbyTracks == null)
				throw new ArgumentException("Audio manifest collections cannot be null.");
			if (manifest.Catalogues.Count > 32 || manifest.Overrides.Count > 4096 || manifest.LobbyTracks.Count > 512)
				throw new ArgumentException("Audio manifest exceeds content limits.");
			if (manifest.Catalogues.Concat(manifest.PlatformCatalogues.Values.SelectMany(value => value))
			    .Concat(manifest.Overrides.Keys).Concat(manifest.Overrides.Values)
			    .Concat(manifest.LobbyTracks)
			    .Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 2048))
				throw new ArgumentException("Audio manifest contains an invalid address.");
			if (manifest.Overrides.Values.Any(manifest.Overrides.ContainsKey))
				throw new ArgumentException("Audio overrides must target physical addresses, not other overrides.");
		}

		private static async Task<string> ResolveCatalogueAsync(string path, bool remoteOnly)
		{
			if (path.StartsWith(LocalTestCataloguePrefix, StringComparison.Ordinal))
			{
				var config = ServerData.ServerConfig;
				if (!Application.isEditor || !config.EnableLocalAudioTest || config.AudioManifest?.Catalogues?.Contains(path) != true)
					throw new ArgumentException("Local audio test content must be enabled in this editor's configuration.");
				string resolved = ResolveLocalTestPath(Application.streamingAssetsPath, path);
				if (!System.IO.File.Exists(resolved)) throw new System.IO.FileNotFoundException("Local audio test catalogue is missing. Rebuild it with Tools/Audio/Lobby Music Test Pack.", resolved);
				return resolved;
			}
			if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http"))
			{
				await ValidateUrlAsync(uri.AbsoluteUri);
				if (uri.AbsolutePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
				{
					path = (await SafeHttpRequest.GetStringAsync(path))?.Trim();
					await ValidateUrlAsync(path);
					return path;
				}

				return uri.AbsoluteUri;
			}

			if (remoteOnly) throw new ArgumentException("Server catalogues must use HTTP(S) URLs.");
			return path;
		}

		public static string ResolveLocalTestPath(string streamingAssetsPath, string catalogue)
		{
			if (!catalogue.StartsWith(LocalTestCataloguePrefix, StringComparison.Ordinal)) throw new ArgumentException("Invalid local audio test catalogue.");
			string relative = Uri.UnescapeDataString(catalogue.Substring(LocalTestCataloguePrefix.Length));
			if (relative.Contains(":") || relative.Contains("\\") || relative.Split('/').Any(part => part == ".." || part == "." || part.Length == 0))
				throw new ArgumentException("Invalid local audio test path.");
			string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(streamingAssetsPath, "AudioModdingTest"));
			string resolved = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, relative));
			if (!resolved.StartsWith(root + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Local audio test path escapes its directory.");
			return resolved;
		}

		private static async Task ValidateUrlAsync(string url)
		{
			if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http") ||
			    !await SafeHttpRequest.ValidateUrlAsync(uri, "Download server audio content"))
				throw new ArgumentException($"Audio download URL was rejected: {url}");
		}

		private static async Task ValidateRemoteLocationsAsync(IEnumerable<IResourceLocation> locations,
			bool remoteOnly)
		{
			var visited = new HashSet<IResourceLocation>();
			var urls = new HashSet<string>();
			var pending = new Stack<IResourceLocation>(locations);
			while (pending.Count > 0)
			{
				var location = pending.Pop();
				if (location.Data is UnityEngine.ResourceManagement.ResourceProviders.AssetBundleRequestOptions options)
				{
					options.Timeout = 30;
					options.RetryCount = 1;
				}

				if (!visited.Add(location)) continue;
				if (location.ResourceType ==
				    typeof(UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource))
				{
					string id = Addressables.ResourceManager.TransformInternalId(location);
					if (id.StartsWith("http", StringComparison.OrdinalIgnoreCase)) urls.Add(id);
					else if (remoteOnly) throw new ArgumentException("Server audio contains a local bundle path.");
				}

				if (location.HasDependencies)
					foreach (var dependency in location.Dependencies)
						pending.Push(dependency);
			}

			foreach (string url in urls) await ValidateUrlAsync(url);
		}

		private static List<IResourceLocation> GetLocations(IResourceLocator locator)
		{
			var result = new HashSet<IResourceLocation>();
			foreach (object key in locator.Keys)
				if (locator.Locate(key, typeof(GameObject), out var locations))
					result.UnionWith(locations);
			return result.ToList();
		}

		private static IResourceLocation ResolveLocation(string address, IEnumerable<IResourceLocator> locators)
		{
			var found = new HashSet<IResourceLocation>();
			foreach (var locator in locators)
				if (locator.Locate(address, typeof(GameObject), out var locations))
					found.UnionWith(locations);
			if (found.Count != 1)
				throw new ArgumentException($"Audio address must identify exactly one prefab: {address}");
			return found.Single();
		}

		public IResourceLocation ResolveAudio(string address)
		{
			bool replaced = overrides.TryGetValue(address, out string replacement);
			address = replaced ? replacement : address;
			var locators = baseline.Values.Concat(session.Values).Distinct().ToList();
			if (!locators.Any(locator => locator.Locate(address, typeof(GameObject), out _)))
			{
				if (builtInLocator != null && builtInLocator.Locate(address, typeof(GameObject), out _))
					return ResolveLocation(address, new[] { builtInLocator });
				if (!replaced) return null;
			}

			return ResolveLocation(address, locators);
		}

		public IEnumerable<string> AudioAddresses => EnumerateAudioAddresses(baseline.Values.Concat(session.Values)
			.Concat(builtInLocator == null ? Enumerable.Empty<IResourceLocator>() : new[] { builtInLocator }));

		public static IEnumerable<string> EnumerateAudioAddresses(IEnumerable<IResourceLocator> locators)
		{
			// Only enumerate materialized catalogue maps. Editor settings locators resolve entries
			// through AssetDatabase and are unsafe to scan (including missing/deleted assets).
			// Labels and GUID aliases are not sound addresses. Custom addresses need not contain '/'.
			return locators.OfType<ResourceLocationMap>().Distinct().SelectMany(locator => locator.Keys.OfType<string>().Where(key =>
				!Guid.TryParse(key, out _) && locator.Locate(key, typeof(GameObject), out var locations) &&
				locations.Any(location => location.PrimaryKey == key))).Distinct();
		}

		private void NotifyAudioRegistryChanged()
		{
			// Content is already committed. An admin UI/discovery failure must not turn a
			// successful load into a failed transaction or prevent other listeners updating.
			try { AdminGlobalAudio.DoLoadAudio(); }
			catch (Exception exception) { Loggy.Error($"Audio discovery refresh failed: {exception}", Category.Addressables); }
			if (AudioRegistryChanged == null) return;
			foreach (Action listener in AudioRegistryChanged.GetInvocationList())
			{
				try { listener(); }
				catch (Exception exception) { Loggy.Error($"Audio registry listener failed: {exception}", Category.Addressables); }
			}
		}

		public void EndServerSession()
		{
			generation++;
			awaitingServerManifest = false;
			LobbyManager.MusicManager.Instance?.SetServerPlaylist(null, false);
			AudioManager.ClearAudioCache();
			foreach (var locator in session.Values.Except(baseline.Values)) Addressables.RemoveResourceLocator(locator);
			session.Clear();
			overrides.Clear();
			FinishLoaded = true;
			NotifyAudioRegistryChanged();
		}
#if UNITY_EDITOR
		public static List<string> GetCataloguePath()
		{
			var path = Application.dataPath.Remove(Application.dataPath.IndexOf("/Assets"));
			//path = path + "/AddressablePackingProjects/SoundAndMusic/ServerData"; //Make OS agnostic
			path = path + "/AddressablePackingProjects";
			if (!System.IO.Directory.Exists(path)) return new List<string>();
			//Loggy.Log(path);
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
						//Loggy.Log(File);
						if (File.EndsWith(".json"))
						{
							if (FoundFile != "")
							{
								Loggy.Error("two catalogues present please only ensure one", Category.Addressables);
							}

							FoundFile = File;
						}
					}

					if (FoundFile == "")
					{
						Loggy.Warning("missing json file", Category.Addressables);
					}
					else
					{
						FoundFiles.Add(FoundFile);
					}
				}
			}

			return FoundFiles;
		}
#endif

		public static List<string> GetCataloguePathStreamingAssets()
		{
			var result = new List<string>();
			foreach (var directory in AccessFile.DirectoriesOrFilesIn("", FolderType.AddressableCatalogues,
				         files: false))
			{
				var files = AccessFile.DirectoriesOrFilesIn(directory, FolderType.AddressableCatalogues)
					.Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).ToList();
				if (files.Count == 0) continue;
				// Build names need not be numeric; choose consistently without parsing exceptions.
				string newest = files.OrderByDescending(path => path, StringComparer.Ordinal).First();
				result.Add(System.IO.Path.Combine(Application.streamingAssetsPath, "AddressableCatalogues", directory,
					newest));
			}

			return result;
		}
	}
}
