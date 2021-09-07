using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using NaughtyAttributes.Editor;

namespace Core.Editor
{
	/// <summary>
	/// <para>This editor tool allows to quickly build the game by disabling unneeded scenes.</para>
	/// It will drastically improve build time.
	/// </summary>
	public class QuickBuild : EditorWindow
	{
		private int tab = 0;

		[MenuItem("Tools/Quick Build", priority = 100)]
		public static void ShowWindow()
		{
			GetWindow<QuickBuild>().Show();
		}

		private void OnEnable()
		{
			// Load persistent settings
			var data = EditorPrefs.GetString(nameof(QuickBuild), JsonUtility.ToJson(this, false));
			JsonUtility.FromJsonOverwrite(data, this);

			projectPath = Directory.GetCurrentDirectory();
			gameManager = AssetDatabase.LoadAssetAtPath<GameManager>("Assets/Prefabs/SceneConstruction/NestedManagers/GameManager.prefab");

			var obj = new SerializedObject(this);
			mainStationProperty = obj.FindProperty(nameof(mainStationScene));
			mainStationProperty.stringValue = mainStationScene;

			if (string.IsNullOrEmpty(buildPath))
			{
				buildPath = Path.Combine(projectPath, "Builds");
			}
			SetPathForDisplay(buildPath);
		}

		private void OnDisable()
		{
			// Save persistent settings
			var data = JsonUtility.ToJson(this, false);
			EditorPrefs.SetString(nameof(QuickBuild), data);
		}

		private void OnGUI()
		{
			tab = GUILayout.Toolbar(tab, new string[] { "Quick Build", "Disable Scenes" });
			switch (tab)
			{
				case 0:
					ShowQuickBuildTab();
					break;
				case 1:
					ShowDisableScenesTab();
					break;
			}
		}

		#region Quick Build Tab

		[SerializeField, Scene] private string mainStationScene = "TestStation";
		[SerializeField] private bool isQuickLoad = true;
		[SerializeField] private BuildTarget target = BuildTarget.StandaloneWindows64;
		[SerializeField] private string buildPath;
		[SerializeField] private bool isDevelopmentBuild = true;
		[SerializeField] private bool isScriptsOnly = false;

		private SerializedProperty mainStationProperty;
		private string projectPath;
		private string pathForDisplay;

		private static readonly string[] requiredScenes = { "StartUp", "Lobby", "OnlineScene" };

		private GameManager gameManager;

		private void ShowQuickBuildTab()
		{
			EditorGUILayout.Space();

			EditorGUILayout.HelpBox("Automatically sets build settings to get MVB " +
					"(minimum viable build) by including only necessary scenes.\n\n" +
					"Operates independently of the main build window. Settings are persistent and won't be picked up by git " +
					"(except Quick Load which is handled externally, so be sure to not commit that change).", MessageType.Info);

			NaughtyEditorGUI.PropertyField_Layout(mainStationProperty, false);
			mainStationScene = mainStationProperty.stringValue;
			ValidateMainStationScene(mainStationScene);

			EditorGUI.BeginChangeCheck();
			var quickLoadLabel = new GUIContent(
				"QuickLoad",
				"At runtime, skips the lobby scene and boots you straight into the map.");
			isQuickLoad = EditorGUILayout.Toggle(quickLoadLabel, isQuickLoad);
			if (EditorGUI.EndChangeCheck())
			{
				UpdateGameManager(isQuickLoad);
			}

			EditorGUILayout.Space();

			target = (BuildTarget)EditorGUILayout.EnumPopup("Target Platform", target);
			if (BuildPipeline.IsBuildTargetSupported(default, target) == false)
			{
				EditorGUILayout.HelpBox("The editor is not configured to target this platform.", MessageType.Warning);
			}

			isDevelopmentBuild = EditorGUILayout.Toggle("Development Build", isDevelopmentBuild);
			if (isDevelopmentBuild)
			{
				var content = new GUIContent(
						"    Scripts Only",
						"Recompiles scripts only. Results in the fastest build, but only works " +
						"if no other assets have been modified since the previous build.");
				isScriptsOnly = EditorGUILayout.Toggle(content, isScriptsOnly);
			}
			else
			{
				GUI.enabled = false;
				isScriptsOnly = EditorGUILayout.Toggle("    Scripts Only", false);
				GUI.enabled = true;
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Location");
			if (GUILayout.Button("Select Path"))
			{
				SelectPath();
			}
			EditorGUILayout.EndHorizontal();

			GUI.enabled = false;
			EditorGUILayout.TextField(pathForDisplay);
			GUI.enabled = true;

			GUILayout.Space(20);

			if (EditorUIUtils.BigAssButton("Build"))
			{
				Build();
			}
		}

		private void ValidateMainStationScene(string sceneName)
		{
			var scene = EditorBuildSettings.scenes.Where((s) => s.path.Contains(sceneName)).FirstOrDefault();
			if (scene != null && scene.path.Contains("MainStations") == false)
			{
				EditorGUILayout.HelpBox(
						"The selected scene doesn't seem to be a main station. Build may fail or be broken.",
						MessageType.Warning);
			}
		}

		private void SetPathForDisplay(string absolutePath)
		{
			pathForDisplay = absolutePath;
			// Try to show path relative to project
			if (pathForDisplay.IndexOf(projectPath, 0, projectPath.Length) == 0)
			{
				var basePath = Path.GetFullPath(Path.Combine(projectPath, "..", ".."));
				if (basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) == false)
				{
					basePath += Path.DirectorySeparatorChar;
				}
				pathForDisplay = pathForDisplay.Remove(0, basePath.Length);
			}
		}

		private void SelectPath()
		{
			var userInput = EditorUtility.SaveFolderPanel("Build Destination", buildPath, "");
			if (string.IsNullOrEmpty(userInput)) return; // Canceling the dialogue retuns null

			// Unity returns linux separator; instead use platform-specific separator
			buildPath = userInput.Replace('/', Path.DirectorySeparatorChar);

			if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
			{
				buildPath = Path.Combine(buildPath, Application.productName + ".exe"); // sigh
			}

			SetPathForDisplay(buildPath);
		}

		private void UpdateGameManager(bool isQuickLoad)
		{
			if (gameManager == null)
			{
				Logger.LogWarning($"{nameof(GameManager)} not found! Cannot set {nameof(GameManager.QuickLoad)} property.");
				return;
			}

			if (gameManager.QuickLoad != isQuickLoad)
			{
				gameManager.QuickLoad = isQuickLoad;
				EditorUtility.SetDirty(gameManager);
				AssetDatabase.SaveAssets();
			}
		}

		private void Build()
		{
			UpdateGameManager(isQuickLoad);

			string[] sceneNames = new string[requiredScenes.Length + 1];
			requiredScenes.CopyTo(sceneNames, 0);
			sceneNames[sceneNames.Length - 1] = mainStationScene;

			var scenePaths = EditorBuildSettings.scenes
					.Where(s => sceneNames.Contains(Path.GetFileNameWithoutExtension(s.path)))
					.Select(s => s.path);

			BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
			{
				scenes = scenePaths.ToArray(),
				locationPathName = buildPath,
				target = target,
				options = BuildOptions.ShowBuiltPlayer
			};

			if (isDevelopmentBuild)
			{
				buildPlayerOptions.options |= BuildOptions.Development;
				if (isScriptsOnly)
				{
					buildPlayerOptions.options |= BuildOptions.BuildScriptsOnly;
				}
			}

			BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
			var timeStr = $"{report.summary.totalTime.Hours}:{report.summary.totalTime.Minutes}:{report.summary.totalTime.Seconds}";

			if (report.summary.result == BuildResult.Succeeded)
			{
				Logger.Log($"Build complete. ({timeStr})");
			}

			if (report.summary.result == BuildResult.Failed)
			{
				Logger.LogError($"Build failed! ({timeStr})");
			}
		}

		#endregion

		#region Disable Scenes Tab

		private static List<string> AdditionalScenesToRemove = new List<string>()
		{
			"Fallstation Centcom",
			"Fallstation Syndicate"
		};

		private string mainStation = "TestStation";
		private bool removeAwaySites = true;
		private bool removeAsteroids = true;
		private bool removeLavaLand = true;
		private bool removeAdditionalScenes = true;

		private void ShowDisableScenesTab()
		{
			GUILayout.Label("Main station:");
			mainStation = GUILayout.TextField(mainStation);
			removeAwaySites = GUILayout.Toggle(removeAwaySites, "Remove away sites");
			removeAsteroids = GUILayout.Toggle(removeAsteroids, "Remove asteroids");
			removeLavaLand = GUILayout.Toggle(removeLavaLand, "Remove LavaLand");
			removeAdditionalScenes = GUILayout.Toggle(removeAdditionalScenes, "Remove Additional Scenes (unsafe for some scenes)");

			if (GUILayout.Button("Disable scenes"))
			{
				StartDisablingScenes();
			}
		}

		public void StartDisablingScenes()
		{
			// remove all stations except TestStation
			RemoveAllStations(exceptStation: mainStation);

			if (removeAsteroids)
				LeaveOneAsteroid();

			if (removeAwaySites)
				LeaveOneAwaySite();

			if (removeLavaLand)
				DisableLavaland();

			if (removeAdditionalScenes)
				DisableAdditionalScenes();

			// make sure that editor saved all changes above
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		/// <summary>
		/// Disable additional scenes in build settings
		/// May cause unexpected behaviour on certain stations/gamemodes
		/// </summary>
		private void DisableAdditionalScenes()
		{
			var currentScenes = EditorBuildSettings.scenes;
			var additionalMaps = currentScenes.Where((scene) =>
			{
			// select all scene to removal
			foreach (var sceneToRemove in AdditionalScenesToRemove)
				{
					if (scene.path.Contains(sceneToRemove))
						return true;
				}

				return false;
			});
			DisableScenes(additionalMaps);
		}

		/// <summary>
		/// Disable LavaLand in json config and build-settings
		/// </summary>
		private void DisableLavaland()
		{
			// Disable LavaLand in config file
			var path = Path.Combine(Application.streamingAssetsPath, "config", "gameConfig.json");
			var config = JsonUtility.FromJson<GameConfig.GameConfig>(File.ReadAllText(path));
			config.SpawnLavaLand = false;

			// save config json
			var gameConfigJson = JsonUtility.ToJson(config);
			File.WriteAllText(path, gameConfigJson);

			// Disable LavaLand scene
			var currentScenes = EditorBuildSettings.scenes;
			var lavaLandMaps = currentScenes.Where((scene) => scene.path.Contains("LavaLand"));
			DisableScenes(lavaLandMaps);
		}

		/// <summary>
		/// Disable all away sites subscenes, except first one
		/// </summary>
		private void LeaveOneAwaySite()
		{
			// get scriptable object with list of all away sites
			var awayWorldsSO = AssetDatabase.LoadAssetAtPath<AwayWorldListSO>("Assets/ScriptableObjects/SubScenes/AwayWorldList.asset");
			if (!awayWorldsSO)
			{
				Logger.LogError("Can't find AwayWorldListSO!", Category.Editor);
				return;
			}

			// remove all away worlds (except first one)
			var firstAwayWorld = awayWorldsSO.AwayWorlds.First();
			awayWorldsSO.AwayWorlds = new List<string> { firstAwayWorld };
			EditorUtility.SetDirty(awayWorldsSO);

			// remove all away worlds from scene list
			var currentScenes = EditorBuildSettings.scenes;
			var awayWorlds = currentScenes.Where((scene) =>
				scene.path.Contains("AwaySites") && !scene.path.Contains(firstAwayWorld))
				.ToList();
			DisableScenes(awayWorlds);
		}

		/// <summary>
		/// Disable all asteroids subscenes, except first one
		/// </summary>
		private static void LeaveOneAsteroid()
		{
			// get scriptable object with list of all asteroids
			var asteroidListSO = AssetDatabase.LoadAssetAtPath<AsteroidListSO>("Assets/ScriptableObjects/SubScenes/AsteroidListSO.asset");
			if (!asteroidListSO)
			{
				Logger.LogError("Can't find AsteroidListSO!", Category.Editor);
				return;
			}

			// remove all except first asteroid
			var firstAsteroid = asteroidListSO.Asteroids.First();
			asteroidListSO.Asteroids = new List<string> { firstAsteroid };
			EditorUtility.SetDirty(asteroidListSO);

			// remove all asteroids from scene list
			var currentScenes = EditorBuildSettings.scenes;
			var asteroids = currentScenes.Where((scene) =>
				scene.path.Contains("AsteroidScenes") && !scene.path.Contains(firstAsteroid));
			DisableScenes(asteroids);
		}

		/// <summary>
		/// Remove all stations from build settings, station scriptable object and json configuration
		/// </summary>
		/// <param name="exceptStation">One station to leave in rotation</param>
		private void RemoveAllStations(string exceptStation)
		{
			var exceptStations = new List<string> { exceptStation };

			// get scriptable object with list of all stations
			var mainStationsSO = AssetDatabase.LoadAssetAtPath<MainStationListSO>("Assets/ScriptableObjects/SubScenes/MainStationList.asset");
			if (!mainStationsSO)
			{
				Logger.LogError("Can't find MainStationListSO!", Category.Editor);
				return;
			}

			// remove all stations from scriptable object 
			mainStationsSO.MainStations = exceptStations;
			EditorUtility.SetDirty(mainStationsSO);

			// replace maps in json configuration in streaming assets
			var mapConfigPath = Path.Combine(Application.streamingAssetsPath, "maps.json");
			var mapConfig = JsonUtility.FromJson<MapList>(File.ReadAllText(mapConfigPath));
			mapConfig.lowPopMaps = exceptStations;
			mapConfig.medPopMaps = exceptStations;
			mapConfig.highPopMaps = exceptStations;

			// save new json
			var mapConfigJson = JsonUtility.ToJson(mapConfig);
			File.WriteAllText(mapConfigPath, mapConfigJson);

			// find stations to delete in editor settings
			var currentScenes = EditorBuildSettings.scenes;
			var mainStations = currentScenes.Where((scene) =>
				scene.path.Contains("MainStations") && !scene.path.Contains(exceptStation));
			DisableScenes(mainStations);
		}

		private static void DisableScenes(IEnumerable<EditorBuildSettingsScene> scenesToDisable)
		{
			var currentScenes = EditorBuildSettings.scenes;
			foreach (var sceneInList in currentScenes)
			{
				foreach (var sceneToDisable in scenesToDisable)
				{
					if (sceneToDisable.guid == sceneInList.guid)
					{
						sceneInList.enabled = false;
					}
				}
			}
			EditorBuildSettings.scenes = currentScenes;
		}

		#endregion
	}
}
