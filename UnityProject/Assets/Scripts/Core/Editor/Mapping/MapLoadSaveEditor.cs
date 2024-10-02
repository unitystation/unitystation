using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using Logs;
using MapSaver;
using Newtonsoft.Json;
using SecureStuff;
using TileManagement;
using Object = UnityEngine.Object;

public class FileSelectorWindow : EditorWindow
{
	private string folderPath = "";
	private string[] fileNames;

	[MenuItem("Mapping/MapLoader_Saver")]
	public static void ShowWindow()
	{
		// Create and show the editor window
		GetWindow<FileSelectorWindow>("File Selector");
	}


	private void OnEnable()
	{
		// Set the default folder path to "Assets/StreamingAssets/Maps"
		folderPath = Path.Combine(Application.dataPath, "StreamingAssets/Maps");

		// Check if the default folder exists, if not, create it
		if (!Directory.Exists(folderPath))
		{
			Directory.CreateDirectory(folderPath);
		}

		// Get all file names from the default folder
		LoadFilesFromFolder();
	}

	private Vector2 scrollPosition = Vector2.zero; // Scroll position variable
	private Color separatorColor = Color.gray; // Define the separator color

	private void OnGUI()
	{
		// Display the selected folder path
		if (!string.IsNullOrEmpty(folderPath))
		{
			GUILayout.Space(5);

			// Display the files in the selected folder
			if (fileNames != null && fileNames.Length > 0)
			{
				GUILayout.Label("Files in Folder:", EditorStyles.boldLabel);

				// Add a scroll view to handle many files
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(1000)); // Adjust the height as needed

				foreach (string fileName in fileNames)
				{
					GUILayout.BeginHorizontal();

					GUILayout.Label(GetRelativePath(folderPath, fileName), GUILayout.Width(400));

					if (GUILayout.Button("Save", GUILayout.Width(50)))
					{
						// Start a coroutine to perform the save function
						Save(fileName);
					}

					if (GUILayout.Button("Load", GUILayout.Width(50)))
					{
						// Start a coroutine to perform the load function
						Load(fileName);
					}

					if (GUILayout.Button("Copy Path", GUILayout.Width(80)))
					{
						// Copy the relative file path to the clipboard
						string relativePath = GetRelativePath(folderPath, fileName);
						EditorGUIUtility.systemCopyBuffer = relativePath;
						Debug.Log("Copied relative path to clipboard: " + relativePath);
					}

					GUILayout.EndHorizontal();

					// Add a colored line separator between each file entry
					Rect rect = GUILayoutUtility.GetRect(1, 1); // Get a rect for the line
					EditorGUI.DrawRect(rect, separatorColor); // Draw the colored separator line
				}

				EditorGUILayout.EndScrollView();
			}
			else
			{
				GUILayout.Label("No files found in the selected folder.", EditorStyles.wordWrappedLabel);
			}
		}
	}


	private string GetRelativePath(string basePath, string fullPath)
	{
		// Ensure both paths use forward slashes
		basePath = basePath.Replace("\\", "/");
		fullPath = fullPath.Replace("\\", "/");

		if (fullPath.StartsWith(basePath))
		{
			// Remove the base path from the full path to get the relative path
			return fullPath.Substring(basePath.Length + 1); // +1 to remove the leading slash
		}

		return fullPath; // If not, return the full path as a fallback
	}

	private void LoadFilesFromFolder()
	{
		if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
		{
			// Get all files from the folder and its subfolders
			fileNames = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
				.Where(x => x.Contains(".meta") == false).ToArray();
		}
		else
		{
			fileNames = new string[0]; // No files found
		}
	}


	private void Load(string filePath)
	{
		MapSaver.MapSaver.CodeClass.ThisCodeClass.Reset();
		MapSaver.MapSaver.MapData mapData =
			JsonConvert.DeserializeObject<MapSaver.MapSaver.MapData>(AccessFile.Load(filePath, FolderType.Maps));
		GameObject go = new GameObject("CoroutineRunner");
		var CoroutineRunnerBehaviour = go.AddComponent<CoroutineRunnerBehaviour>();
		CoroutineRunnerBehaviour.StartCoroutine(MapLoader.ServerLoadMap(Vector3.zero, Vector3.zero, mapData));
	}


	public List<MetaTileMap> SortObjectsByChildIndex(List<MetaTileMap> objects)
	{
		// Sort the objects based on their sibling index
		objects.Sort((x, y) => y.transform.parent.GetSiblingIndex().CompareTo(x.transform.parent.GetSiblingIndex()));

		// Return the sorted list
		return objects;
	}

	private void Save(string filePath)
	{
		try
		{
			var MapMatrices = Object.FindObjectsByType<MetaTileMap>(FindObjectsSortMode.None).ToList();

			// Sort objects by their recursive child index path
			MapMatrices = SortObjectsByChildIndex(MapMatrices);

			if (MapMatrices.Count == 0)
			{
				Loggy.LogError($"No maps found for Save {filePath}");
				return;
			}

			MapMatrices.Reverse();

			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore, // Ignore null values
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, // Ignore default values
				Formatting = Formatting.Indented
			};
			var Map = MapSaver.MapSaver.SaveMap(MapMatrices, false, MapMatrices[0].name);
			AccessFile.Save(filePath, JsonConvert.SerializeObject(Map, settings), FolderType.Maps);
			EditorUtility.DisplayDialog("Save Complete", $"Map saved successfully to {filePath}.", "OK");
		}
		catch (Exception e)
		{
			Loggy.LogError(e.ToString());
		}
	}
}