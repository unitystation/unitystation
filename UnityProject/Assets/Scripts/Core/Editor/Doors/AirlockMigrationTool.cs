using System.Collections.Generic;
using System.IO;
using Systems.Clearance;
using Systems.Clearance.Utils;
using Doors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WebSocketSharp;

namespace Core.Editor.Doors
{
	public class AirlockMigrationTool: EditorWindow
	{
		private int tab = 0;
		private string log = "";
		private Vector2 scrollPosition;
		private string prefabsFolder;

		[MenuItem("Tools/Migrations/Airlock Clearance Migration")]
		public static void ShowWindow()
		{
			GetWindow<AirlockMigrationTool>().Show();
		}

		private void OnGUI()
		{
			tab = GUILayout.Toolbar(tab, new[] { "Folder", "Scene" });

			switch (tab)
			{
				case 0:
					ShowFolderTab();
					break;
				case 1:
					ShowSceneTab();
					break;
			}

			EditorGUILayout.Space();
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,GUILayout.Height(100));
			EditorGUILayout.TextArea(log);
			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}

		private void ShowFolderTab()
		{
			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("By pressing the button, we will try to migrate" +
			                        " Access -> Clearance on the specified folder", MessageType.Info);

			if (GUILayout.Button("Folder"))
			{
				SelectPath();
			}

			if (GUILayout.Button("Migrate!"))
			{
				MigrateDoorsInFolder();
			}
		}

		private void ShowSceneTab()
		{
			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("By pressing the button, we will try to migrate" +
			                        " Acces -> Clearance on the current loaded scene", MessageType.Info);


			if (GUILayout.Button("Migrate!"))
			{
				MigrateDoorsInScene();
			}

		}

		private void AddToLog(string newLine)
		{
			log += newLine + "\n";
		}

		private void ClearLog()
		{
			log = string.Empty;
		}

		private void SelectPath()
		{
			if (prefabsFolder.IsNullOrEmpty())
			{
				var sep = Path.DirectorySeparatorChar;
				prefabsFolder = $"Assets{sep}Prefabs{sep}Doors{sep}MachineDoors{sep}Airlocks";
			}
			var userInput = EditorUtility.SaveFolderPanel("Folder to prefabs", prefabsFolder, "");

			prefabsFolder = userInput.Replace('/', Path.DirectorySeparatorChar);
		}

		private void MigrateDoorsInFolder()
		{
			ClearLog();
			if (prefabsFolder.IsNullOrEmpty())
			{
				SelectPath();
			}

			var dir = new DirectoryInfo(prefabsFolder);
			var files = dir.GetFiles("*.prefab");

			foreach (var file in files)
			{
				var relPath = prefabsFolder.Replace(Application.dataPath, "Assets");
				var s = $"{relPath}{Path.DirectorySeparatorChar}{file.Name}";
				AddToLog($"Trying to load {file.Name} on {s}");
				var pgo = AssetDatabase.LoadAssetAtPath(s, typeof(GameObject)) as GameObject;

				if (pgo is null)
				{
					AddToLog($"Couldn't process {file.Name}! Skipping.");
					continue;
				}
				AddToLog($"Processing {pgo.name}...");
				var go = PrefabUtility.InstantiatePrefab(pgo) as GameObject;
				go = ReplaceGoClearance(go);
				bool success;
				PrefabUtility.SaveAsPrefabAsset(go, s, out success);
				AddToLog(success ? "Migrated successfully" : "Couldn't migrate!");
				DestroyImmediate(go);
			}
		}

		private GameObject ReplaceGoClearance(GameObject instance)
		{
			var clearanceCheckable = instance.GetComponentInChildren<ClearanceCheckable>();
			var accessRestrictions = instance.GetComponentInChildren<AccessRestrictions>();
			if (clearanceCheckable && accessRestrictions)
			{
				if (accessRestrictions.restriction != 0)
				{
					var clear = new List<Clearance> { MigrationData.Translation[accessRestrictions.restriction]};
					EditorUtility.SetDirty(instance);
					clearanceCheckable.SetClearance(clear);
				}
			}
			return instance;
		}

		private void MigrateDoorsInScene()
		{
			ClearLog();
			var doors = GameObject.FindObjectsOfType<DoorMasterController>();
			AddToLog($"Migrating airlocks from {EditorSceneManager.GetActiveScene().name}");
			foreach (var door in doors)
			{
				AddToLog($"processing {door.name}...");
				var clearanceCheckable = door.GetComponentInChildren<ClearanceCheckable>();
				var accessRestrictions = door.GetComponentInChildren<AccessRestrictions>();
				if (clearanceCheckable && accessRestrictions)
				{
					if (accessRestrictions.restriction != 0)
					{
						var clear = new List<Clearance> { MigrationData.Translation[accessRestrictions.restriction]};
						Undo.RecordObject(clearanceCheckable, "Clearance list update");
						clearanceCheckable.SetClearance(clear);
					}
				}
				else
				{
					AddToLog($"There is no access module in {door.name}. Skipping!");
				}
				AddToLog("done!");
				AddToLog("Remember to save changes with control+s!");
			}
		}

	}
}