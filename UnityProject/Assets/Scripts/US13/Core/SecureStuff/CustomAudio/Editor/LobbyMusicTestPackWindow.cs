#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace US13.Core.SecureStuff.CustomAudio.Editor
{
	public class LobbyMusicTestPackWindow : EditorWindow
	{
		private const string GeneratedRoot = "Assets/EditorGenerated/LobbyAudioTest";
		private const string BackupPath = "Library/LobbyAudioTest/config-backup.json";
		[SerializeField] private List<string> files = new List<string>();
		[SerializeField] private bool replacePlaylist = true;
		[SerializeField] private bool mirrorPack = true;
		private Vector2 scroll;
		private string status;

		[MenuItem("Tools/Audio/Lobby Music Test Pack")]
		public static void Open() => GetWindow<LobbyMusicTestPackWindow>("Lobby Music Test Pack");

		private void OnGUI()
		{
			EditorGUILayout.HelpBox("Choose MP3 files or drag them here. Mirror mode builds into CustomAudio and configures the server playlist; host a game to transfer it to clients. Local mode tests in the editor lobby. Restore Config restores your previous audio configuration.", MessageType.Info);
			if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				if (Event.current.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();
					foreach (string path in DragAndDrop.paths) AddFile(path);
				}
				Event.current.Use();
			}
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Add MP3")) AddFile(EditorUtility.OpenFilePanel("Choose lobby music", "", "mp3"));
			if (GUILayout.Button("Add Folder"))
			{
				string folder = EditorUtility.OpenFolderPanel("Choose MP3 folder", "", "");
				if (!string.IsNullOrEmpty(folder)) foreach (string path in Directory.GetFiles(folder).Where(IsMp3)) AddFile(path);
			}
			if (GUILayout.Button("Clear")) files.Clear();
			EditorGUILayout.EndHorizontal();
			scroll = EditorGUILayout.BeginScrollView(scroll);
			for (int i = 0; i < files.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(new GUIContent(Path.GetFileName(files[i]), files[i]));
				if (GUILayout.Button("Remove", GUILayout.Width(65))) files.RemoveAt(i--);
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndScrollView();
			replacePlaylist = EditorGUILayout.Toggle("Replace lobby playlist", replacePlaylist);
			mirrorPack = EditorGUILayout.Toggle("Transfer through Mirror", mirrorPack);
			EditorGUILayout.LabelField("Build target", EditorUserBuildSettings.activeBuildTarget.ToString());
			using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer))
			{
				using (new EditorGUI.DisabledScope(files.Count == 0))
					if (GUILayout.Button("Build Pack and Configure Test")) Run(() => BuildPack(files, replacePlaylist, mirrorPack));
				using (new EditorGUI.DisabledScope(!File.Exists(BackupPath)))
					if (GUILayout.Button("Restore Previous Config")) Run(() => { RestoreConfiguration(); return "Previous audio configuration restored. Built packs are retained for inspection."; });
			}
			if (GUILayout.Button("Show Generated Packs"))
			{
				string output = Path.Combine(Application.streamingAssetsPath, mirrorPack ? "CustomAudio" : "AudioModdingTest");
				if (Directory.Exists(output)) EditorUtility.RevealInFinder(output);
			}
			if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
		}

		private void Run(Func<string> action)
		{
			try { status = action(); Debug.Log(status); }
			catch (Exception exception) { status = exception.Message; Debug.LogException(exception); }
			finally { EditorUtility.ClearProgressBar(); }
		}

		private static bool IsMp3(string path) => string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase);
		private void AddFile(string path)
		{
			if (string.IsNullOrEmpty(path) || !IsMp3(path)) return;
			path = Path.GetFullPath(path);
			if (!files.Contains(path)) files.Add(path);
		}

		public static string BuildPack(IReadOnlyList<string> mp3Files, bool replace, bool transferThroughMirror = false)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before building a test pack.");
			if (mp3Files == null || mp3Files.Count == 0) throw new ArgumentException("Choose at least one MP3 file.");
			foreach (string path in mp3Files)
				if (!File.Exists(path) || !IsMp3(path)) throw new ArgumentException("Missing or invalid MP3: " + path);
			string configPath = Path.Combine(Application.streamingAssetsPath, "Config/config.json");
			var config = JObject.Parse(File.ReadAllText(configPath));
			var backup = File.Exists(BackupPath) ? JObject.Parse(File.ReadAllText(BackupPath)) : CaptureConfiguration(config);
			if (File.Exists(BackupPath)) VerifyInstalledConfiguration(config, backup);
			string id = Guid.NewGuid().ToString("N");
			string sourceRoot = GeneratedRoot + "/" + id;
			string outputFolder = transferThroughMirror ? "CustomAudio" : "AudioModdingTest";
			string output = Path.Combine(Application.streamingAssetsPath, outputFolder, id);
			Directory.CreateDirectory(sourceRoot);
			Directory.CreateDirectory(output);
			var assetPaths = new List<string>();
			var addresses = new List<string>();
			try
			{
				AssetDatabase.Refresh();
				for (int i = 0; i < mp3Files.Count; i++)
				{
					EditorUtility.DisplayProgressBar("Lobby audio test pack", "Importing " + Path.GetFileName(mp3Files[i]), (float)i / mp3Files.Count);
					string clipPath = sourceRoot + "/" + i + "_" + Path.GetFileName(mp3Files[i]);
					File.Copy(mp3Files[i], clipPath);
					AssetDatabase.ImportAsset(clipPath, ImportAssetOptions.ForceSynchronousImport);
					var importer = (AudioImporter)AssetImporter.GetAtPath(clipPath);
					if (importer == null) throw new InvalidOperationException("Unity could not import " + mp3Files[i]);
					var samples = importer.defaultSampleSettings;
					samples.loadType = AudioClipLoadType.Streaming;
					samples.compressionFormat = AudioCompressionFormat.Vorbis;
					importer.defaultSampleSettings = samples;
					importer.SaveAndReimport();
					var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
					if (clip == null) throw new InvalidOperationException("Unity could not import " + mp3Files[i]);
					var obj = new GameObject(Path.GetFileNameWithoutExtension(mp3Files[i]));
					try
					{
						var source = obj.AddComponent<AudioSource>();
						source.clip = clip;
						source.playOnAwake = false;
						source.loop = false;
						source.spatialBlend = 0f;
						string prefabPath = sourceRoot + "/track_" + i + ".prefab";
						if (PrefabUtility.SaveAsPrefabAsset(obj, prefabPath) == null) throw new InvalidOperationException("Could not create audio prefab.");
						assetPaths.Add(prefabPath);
						addresses.Add("local-lobby-test/" + id + "/track_" + i);
					}
					finally { DestroyImmediate(obj); }
				}
				AssetDatabase.SaveAssets();
				EditorUtility.DisplayProgressBar("Lobby audio test pack", "Building audio bundle", 0.8f);
				string bundleName = "lobby_music_" + id + ".bundle";
				var built = BuildPipeline.BuildAssetBundles(output,
					new[] { new AssetBundleBuild { assetBundleName = bundleName, assetNames = assetPaths.ToArray() } },
					BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
				if (built == null) throw new InvalidOperationException("Audio bundle build failed. See the Console.");
				string bundlePath = Path.Combine(output, bundleName);
				if (!BuildPipeline.GetCRCForAssetBundle(bundlePath, out uint crc)) throw new InvalidOperationException("Could not read bundle CRC.");
				var options = new AssetBundleRequestOptions
				{
					BundleName = bundleName, Hash = built.GetAssetBundleHash(bundleName).ToString(), Crc = crc,
					BundleSize = new FileInfo(bundlePath).Length, Timeout = 30, RetryCount = 1
				};
				string bundleKey = "local-lobby-test-bundle/" + id;
				var entries = new List<ContentCatalogDataEntry>
				{
					new ContentCatalogDataEntry(typeof(IAssetBundleResource),
						"{UnityEngine.Application.streamingAssetsPath}/" + outputFolder + "/" + id + "/" + bundleName,
						typeof(AssetBundleProvider).FullName, new object[] { bundleKey }, extraData: options)
				};
				for (int i = 0; i < addresses.Count; i++)
					entries.Add(new ContentCatalogDataEntry(typeof(GameObject), assetPaths[i], typeof(BundledAssetProvider).FullName,
						new object[] { addresses[i] }, new object[] { bundleKey }));
				var catalog = new ContentCatalogData(entries, "LobbyMusicTest_" + id)
				{
					InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData<InstanceProvider>(),
					SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData<SceneProvider>(),
					ResourceProviderData = new List<ObjectInitializationData>
					{
						ObjectInitializationData.CreateSerializedInitializationData<AssetBundleProvider>(),
						ObjectInitializationData.CreateSerializedInitializationData<BundledAssetProvider>()
					}
				};
				string json = JsonUtility.ToJson(catalog);
				File.WriteAllText(Path.Combine(output, "catalog.json"), json);
				File.WriteAllText(Path.Combine(output, "catalog.hash"), Hash128.Compute(json).ToString());
				var manifest = new ServerAudioManifest
				{
					Revision = 1, Catalogues = transferThroughMirror ? new List<string>() : new List<string> { CustomAudioPack.LocalTestCataloguePrefix + id + "/catalog.json" },
					LobbyTracks = addresses, ReplaceLobbyPlaylist = replace
				};
				config["AudioManifest"] = JObject.FromObject(manifest);
				config["EnableLocalAudioTest"] = !transferThroughMirror;
				backup["InstalledManifest"] = config["AudioManifest"].DeepClone();
				backup["InstalledEnabled"] = !transferThroughMirror;
				Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
				File.WriteAllText(BackupPath, backup.ToString());
				File.WriteAllText(configPath, config.ToString());
				AssetDatabase.Refresh();
				return $"Built {addresses.Count} lobby tracks for {EditorUserBuildSettings.activeBuildTarget}. " +
					(transferThroughMirror ? "Start a host/server to test Mirror transfer. " : "Start Play Mode to hear the test playlist. ") + $"Pack: {output}";
			}
			finally
			{
				AssetDatabase.DeleteAsset(sourceRoot);
				EditorUtility.ClearProgressBar();
			}
		}

		private static JObject CaptureConfiguration(JObject config) => new JObject
		{
			["HadManifest"] = config.Property("AudioManifest") != null,
			["PreviousManifest"] = config["AudioManifest"]?.DeepClone(),
			["HadEnabled"] = config.Property("EnableLocalAudioTest") != null,
			["PreviousEnabled"] = config["EnableLocalAudioTest"]?.DeepClone()
		};

		private static void VerifyInstalledConfiguration(JObject config, JObject backup)
		{
			if (!JToken.DeepEquals(config["AudioManifest"], backup["InstalledManifest"]) ||
			    config["EnableLocalAudioTest"]?.Value<bool>() != (backup["InstalledEnabled"]?.Value<bool>() ?? true))
				throw new InvalidOperationException("Audio test config was edited after the last build. Preserve those edits before restoring or rebuilding; the original field backup is in " + BackupPath);
		}

		public static void RestoreConfiguration()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before restoring configuration.");
			string configPath = Path.Combine(Application.streamingAssetsPath, "Config/config.json");
			var config = JObject.Parse(File.ReadAllText(configPath));
			var backup = JObject.Parse(File.ReadAllText(BackupPath));
			VerifyInstalledConfiguration(config, backup);
			if (backup["HadManifest"].Value<bool>()) config["AudioManifest"] = backup["PreviousManifest"].DeepClone(); else config.Remove("AudioManifest");
			if (backup["HadEnabled"].Value<bool>()) config["EnableLocalAudioTest"] = backup["PreviousEnabled"].DeepClone(); else config.Remove("EnableLocalAudioTest");
			File.WriteAllText(configPath, config.ToString());
			File.Delete(BackupPath);
			AssetDatabase.Refresh();
		}
	}
}
#endif
