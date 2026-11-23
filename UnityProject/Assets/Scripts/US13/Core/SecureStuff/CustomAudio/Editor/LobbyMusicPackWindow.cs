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
	public class LobbyMusicPackWindow : EditorWindow
	{
		private const string GeneratedRoot = "Assets/EditorGenerated/LobbyAudio";
		private const string BackupPath = "Library/LobbyAudio/config-backup.json";
		private const string LegacyBackupPath = "Library/LobbyAudioTest/config-backup.json";
		private static string ExistingBackupPath => File.Exists(BackupPath) ? BackupPath : LegacyBackupPath;
		[SerializeField] private List<string> files = new List<string>();
		[SerializeField] private string bundleName = "lobby-music";
		[SerializeField] private bool replacePlaylist = true;
		[SerializeField] private bool mirrorPack = true;
		private Vector2 scroll;
		private string status;

		[MenuItem("Tools/Audio/Lobby Music Pack")]
		public static void Open() => GetWindow<LobbyMusicPackWindow>("Lobby Music Pack");

		private void OnGUI()
		{
			EditorGUILayout.HelpBox("Choose MP3 files or drag them here. Mirror mode builds into CustomAudio and configures the server playlist; host a game to transfer it to clients. Local mode loads the pack in the editor lobby. Restore Config restores your previous audio configuration.", MessageType.Info);
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
			bundleName = EditorGUILayout.TextField(new GUIContent("Bundle name", "Used for the .bundle filename. Supports letters, numbers, spaces, hyphens and underscores."), bundleName);
			replacePlaylist = EditorGUILayout.Toggle("Replace lobby playlist", replacePlaylist);
			mirrorPack = EditorGUILayout.Toggle("Transfer through Mirror", mirrorPack);
			EditorGUILayout.LabelField("Build target", EditorUserBuildSettings.activeBuildTarget.ToString());
			using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer))
			{
				using (new EditorGUI.DisabledScope(files.Count == 0))
					if (GUILayout.Button("Build Pack and Configure")) Run(() => BuildPack(files, replacePlaylist, mirrorPack, bundleName));
				using (new EditorGUI.DisabledScope(!File.Exists(ExistingBackupPath)))
					if (GUILayout.Button("Restore Previous Config")) Run(() => { RestoreConfiguration(); return "Previous audio configuration restored. Built packs are retained for inspection."; });
			}
			if (GUILayout.Button("Show Generated Packs"))
			{
				string output = Path.Combine(Application.streamingAssetsPath, mirrorPack ? "CustomAudio" : CustomAudioPack.LocalAudioFolder);
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

		public static string NormaliseBundleName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Enter a bundle name.", nameof(name));
			name = name.Trim().ToLowerInvariant();
			if (name.EndsWith(".bundle", StringComparison.Ordinal)) name = name.Substring(0, name.Length - 7);
			name = string.Join("-", name.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
			if (name.Length == 0 || name.Length > 64 || name.Any(c => !(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-' || c == '_')))
				throw new ArgumentException("Use 1–64 letters, numbers, hyphens or underscores for the bundle name.", nameof(name));
			var reserved = new[] { "con", "prn", "aux", "nul", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9" };
			if (reserved.Contains(name)) throw new ArgumentException("This bundle name is reserved by the filesystem.", nameof(name));
			return name;
		}

		public static string BuildPack(IReadOnlyList<string> mp3Files, bool replace, bool transferThroughMirror = false, string name = "lobby-music")
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before building an audio pack.");
			if (mp3Files == null || mp3Files.Count == 0) throw new ArgumentException("Choose at least one MP3 file.");
			foreach (string path in mp3Files)
				if (!File.Exists(path) || !IsMp3(path)) throw new ArgumentException("Missing or invalid MP3: " + path);
			string configPath = Path.Combine(Application.streamingAssetsPath, "Config/config.json");
			var config = JObject.Parse(File.ReadAllText(configPath));
			string packName = NormaliseBundleName(name);
			string outputFolder = transferThroughMirror ? "CustomAudio" : CustomAudioPack.LocalAudioFolder;
			string baseId = packName + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
			string id = baseId;
			for (int suffix = 2; Directory.Exists(Path.Combine(Application.streamingAssetsPath, outputFolder, id)) || Directory.Exists(GeneratedRoot + "/" + id); suffix++) id = baseId + "-" + suffix;
			string sourceRoot = GeneratedRoot + "/" + id;
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
					EditorUtility.DisplayProgressBar("Lobby music pack", "Importing " + Path.GetFileName(mp3Files[i]), (float)i / mp3Files.Count);
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
						addresses.Add("lobby-music/" + id + "/track_" + i);
					}
					finally { DestroyImmediate(obj); }
				}
				AssetDatabase.SaveAssets();
				EditorUtility.DisplayProgressBar("Lobby music pack", "Building audio bundle", 0.8f);
				string bundleName = packName + ".bundle";
				// Unity identifies loaded bundles by their internal name, independently of the filename.
				string internalBundleName = id + ".bundle";
				var built = BuildPipeline.BuildAssetBundles(output,
					new[] { new AssetBundleBuild { assetBundleName = internalBundleName, assetNames = assetPaths.ToArray() } },
					BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
				if (built == null) throw new InvalidOperationException("Audio bundle build failed. See the Console.");
				string bundlePath = Path.Combine(output, bundleName);
				File.Move(Path.Combine(output, internalBundleName), bundlePath);
				string bundleManifest = Path.Combine(output, internalBundleName + ".manifest");
				if (File.Exists(bundleManifest)) File.Move(bundleManifest, bundlePath + ".manifest");
				if (!BuildPipeline.GetCRCForAssetBundle(bundlePath, out uint crc)) throw new InvalidOperationException("Could not read bundle CRC.");
				var options = new AssetBundleRequestOptions
				{
					BundleName = internalBundleName, Hash = built.GetAssetBundleHash(internalBundleName).ToString(), Crc = crc,
					BundleSize = new FileInfo(bundlePath).Length, Timeout = 30, RetryCount = 1
				};
				string bundleKey = "lobby-music-bundle/" + id;
				var entries = new List<ContentCatalogDataEntry>
				{
					new ContentCatalogDataEntry(typeof(IAssetBundleResource),
						"{UnityEngine.Application.streamingAssetsPath}/" + outputFolder + "/" + id + "/" + bundleName,
						typeof(AssetBundleProvider).FullName, new object[] { bundleKey }, extraData: options)
				};
				for (int i = 0; i < addresses.Count; i++)
					entries.Add(new ContentCatalogDataEntry(typeof(GameObject), assetPaths[i], typeof(BundledAssetProvider).FullName,
						new object[] { addresses[i] }, new object[] { bundleKey }));
				var catalog = new ContentCatalogData(entries, "LobbyMusic_" + id)
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
					Revision = 1, Catalogues = transferThroughMirror ? new List<string>() : new List<string> { CustomAudioPack.LocalCataloguePrefix + id + "/catalog.json" },
					LobbyTracks = addresses, ReplaceLobbyPlaylist = replace
				};
				config = JObject.Parse(File.ReadAllText(configPath));
				string previousBackupPath = ExistingBackupPath;
				var previousBackup = File.Exists(previousBackupPath) ? JObject.Parse(File.ReadAllText(previousBackupPath)) : null;
				bool newRestorePoint = previousBackup != null && !MatchesInstalledConfiguration(config, previousBackup);
				var backup = previousBackup != null && !newRestorePoint ? previousBackup : CaptureConfiguration(config);
				config["AudioManifest"] = JObject.FromObject(manifest);
				config[CustomAudioPack.LocalAudioEnabledKey] = !transferThroughMirror;
				config.Remove(CustomAudioPack.LegacyLocalAudioEnabledKey);
				backup["InstalledManifest"] = config["AudioManifest"].DeepClone();
				backup["InstalledEnabled"] = !transferThroughMirror;
				Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
				if (newRestorePoint) File.WriteAllText(Path.Combine(Path.GetDirectoryName(BackupPath), "config-backup-" + Guid.NewGuid().ToString("N") + ".json"), previousBackup.ToString());
				File.WriteAllText(BackupPath, backup.ToString());
				File.WriteAllText(configPath, config.ToString());
				if (previousBackupPath != BackupPath && File.Exists(previousBackupPath)) File.Delete(previousBackupPath);
				AssetDatabase.Refresh();
				return $"Built {addresses.Count} lobby tracks for {EditorUserBuildSettings.activeBuildTarget}. " +
					(transferThroughMirror ? "Start a host/server to distribute the pack through Mirror. " : "Start Play Mode to hear the playlist. ") + $"Pack: {output}";
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
			["EnabledKey"] = config.Property(CustomAudioPack.LocalAudioEnabledKey) != null ? CustomAudioPack.LocalAudioEnabledKey : CustomAudioPack.LegacyLocalAudioEnabledKey,
			["HadEnabled"] = config.Property(CustomAudioPack.LocalAudioEnabledKey) != null || config.Property(CustomAudioPack.LegacyLocalAudioEnabledKey) != null,
			["PreviousEnabled"] = (config[CustomAudioPack.LocalAudioEnabledKey] ?? config[CustomAudioPack.LegacyLocalAudioEnabledKey])?.DeepClone()
		};

		private static bool MatchesInstalledConfiguration(JObject config, JObject backup) =>
			JToken.DeepEquals(config["AudioManifest"], backup["InstalledManifest"]) &&
			(config[CustomAudioPack.LocalAudioEnabledKey] ?? config[CustomAudioPack.LegacyLocalAudioEnabledKey])?.Value<bool>() == (backup["InstalledEnabled"]?.Value<bool>() ?? true);

		private static void VerifyInstalledConfiguration(JObject config, JObject backup)
		{
			if (!MatchesInstalledConfiguration(config, backup))
				throw new InvalidOperationException("Audio settings changed after the last pack build. Rebuild to use the current settings as a new restore point. Earlier backups are retained in " + Path.GetDirectoryName(BackupPath) + ".");
		}

		public static void RestoreConfiguration()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before restoring configuration.");
			string configPath = Path.Combine(Application.streamingAssetsPath, "Config/config.json");
			var config = JObject.Parse(File.ReadAllText(configPath));
			string backupPath = ExistingBackupPath;
			var backup = JObject.Parse(File.ReadAllText(backupPath));
			VerifyInstalledConfiguration(config, backup);
			if (backup["HadManifest"].Value<bool>()) config["AudioManifest"] = backup["PreviousManifest"].DeepClone(); else config.Remove("AudioManifest");
			config.Remove(CustomAudioPack.LocalAudioEnabledKey);
			config.Remove(CustomAudioPack.LegacyLocalAudioEnabledKey);
			if (backup["HadEnabled"].Value<bool>()) config[backup["EnabledKey"]?.Value<string>() ?? CustomAudioPack.LegacyLocalAudioEnabledKey] = backup["PreviousEnabled"].DeepClone();
			File.WriteAllText(configPath, config.ToString());
			File.Delete(backupPath);
			AssetDatabase.Refresh();
		}
	}
}
#endif
