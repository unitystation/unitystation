using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SecureStuff;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class CompresMaps : IPreprocessBuild
{
	public int callbackOrder
	{
		get { return 11; }
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

	public void OnPreprocessBuild(BuildTarget target, string path)
	{
		var Gamedata = AssetDatabase.LoadAssetAtPath<GameObject>(
			"Assets/Prefabs/SceneConstruction/NestedManagers/GameData.prefab");
		if (Gamedata.GetComponent<GameData>().DevBuild)
		{
			return;
		}

		Stopwatch stopwatc = new Stopwatch();
		stopwatc.Start();
		// Set the default folder path to "Assets/StreamingAssets/Maps"
		var folderPath = Path.Combine(Application.dataPath, "StreamingAssets/Maps");

		// Check if the default folder exists, if not, create it
		if (Directory.Exists(folderPath) == false)
		{
			Directory.CreateDirectory(folderPath);
		}

		string[] fileNames;
		if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
		{
			// Get all files from the folder and its subfolders
			fileNames = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).Where(x => x.Contains(".meta") == false).ToArray();
		}
		else
		{
			fileNames = new string[0]; // No files found
		}

		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore, // Ignore null values
			DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, // Ignore default values
			Formatting = Formatting.Indented
		};

		settings.Formatting = Formatting.None;

		foreach (var fileName in fileNames)
		{
			var RelativePath = GetRelativePath(folderPath, fileName);
			var MapData = AccessFile.Load(RelativePath, FolderType.Maps);
			MapSaver.MapSaver.MapData mapData = JsonConvert.DeserializeObject<MapSaver.MapSaver.MapData>(MapData);
			if (mapData.ConvertToCompact())
			{
				//was non-compact
				var SaveString = JsonConvert.SerializeObject(mapData, settings);
				//StringBuilder sb = new StringBuilder(SaveString);


				// foreach (var KVP in GitDItoIDs)
				// {
				// 	sb.Replace(KVP.Key, KVP.Value);
				// }
				AccessFile.Save(RelativePath, SaveString,FolderType.Maps);
			}
		}

		stopwatc.Stop();
		Debug.Log($"CompresMaps stopwatc took ElapsedMilliseconds {stopwatc.ElapsedMilliseconds}");
		//onvertToCompactor()
	}
}
