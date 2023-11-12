using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Logs;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using NaughtyAttributes;
using NaughtyAttributes.Editor;
using Shared.Editor;

namespace Core.Editor
{
	/// <summary>
	/// <para>This editor tool allows to quickly build the game by disabling unneeded scenes.</para>
	/// It will drastically improve build time.
	/// </summary>
	public class QuickBuild : EditorWindow
	{
		private int tab = 0;

		[MenuItem("Tools/Quick Build", priority = 2)]
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
			ShowQuickBuildTab();
		}

		[SerializeField, Scene] private string mainStationScene = "TestStation";
		[SerializeField] private BuildTarget target = BuildTarget.StandaloneWindows64;
		[SerializeField] private string buildPath;
		[SerializeField] private bool isDevelopmentBuild = true;
		[SerializeField] private bool isScriptsOnly = false;

		private SerializedProperty mainStationProperty;
		private string projectPath;
		private string pathForDisplay;

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
					"Quick Load",
					"At runtime, skips the lobby scene and boots you straight into the map.");


			EditorGUILayout.Toggle(quickLoadLabel, QuickLoad.IsEnabled);

			if (EditorGUI.EndChangeCheck())
			{
				QuickLoad.Toggle();
			}

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

			if (EditorUIUtils.BigButton("Build"))
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

		private void Build()
		{
			string[] sceneNames = { "StartUp", "Lobby", "OnlineScene", "SpaceScene", mainStationScene };
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
				Loggy.Log($"Build complete. ({timeStr})");
			}

			if (report.summary.result == BuildResult.Failed)
			{
				Loggy.LogError($"Build failed! ({timeStr})");
			}
		}
	}
}
