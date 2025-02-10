using System;
using System.Collections;
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
using Util;
using Object = UnityEngine.Object;

public class FileSelectorWindow : EditorWindow
{
    private string folderPath = "";
    private string[] fileNames;
    private string customSavePath = "";
    private string customJsonInput = "";

    [MenuItem("Mapping/𓃡𓃡 Map Loader Saver Selector 𓃡𓃡")]
    public static void ShowWindow()
    {
        GetWindow<FileSelectorWindow>("𓃡𓃡 Map Loader Saver Selector 𓃡𓃡");
    }

    private const string SelectedMap = "SelectedMap";
    private static bool DeleteMapAfterSave = false;
    private Vector2 scrollPosition = Vector2.zero;
    private Color separatorColor = Color.gray;

    private void OnEnable()
    {
        if (EditorPrefs.HasKey(SelectedMap))
        {
            SubSceneManager.AdminForcedMainStation = EditorPrefs.GetString(SelectedMap);
        }
        folderPath = Path.Combine(Application.dataPath, "StreamingAssets/Maps");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        LoadFilesFromFolder();
    }

    private void OnGUI()
    {
        GUILayout.Label("Delete Map After Save ", EditorStyles.boldLabel);
        DeleteMapAfterSave = GUILayout.Toggle(DeleteMapAfterSave, "", GUILayout.Width(20));

        GUILayout.Space(10);
        GUILayout.Label("Standalone Save As", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        customSavePath = EditorGUILayout.TextField("Save Path:", customSavePath);
        if (GUILayout.Button("Browse"))
        {
            customSavePath = EditorUtility.SaveFilePanel("Save Map As", folderPath, "map.json", "json");
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Custom JSON Input", EditorStyles.boldLabel);
        customJsonInput = EditorGUILayout.TextArea(customJsonInput, GUILayout.Height(100));

        if (GUILayout.Button("Save As"))
        {
            if (!string.IsNullOrEmpty(customSavePath))
            {
                Save(customSavePath);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please specify a valid save path.", "OK");
            }
        }

        if (GUILayout.Button("Load Custom JSON"))
        {
	        if (!string.IsNullOrEmpty(customJsonInput))
	        {
		        Load("");
	        }
	        else
	        {
		        EditorUtility.DisplayDialog("Error", "Custom JSON input is empty.", "OK");
	        }
        }

        GUILayout.Space(10);

        if (!string.IsNullOrEmpty(folderPath))
        {
            GUILayout.Space(5);
            if (fileNames != null && fileNames.Length > 0)
            {
                GUILayout.Label("Files in Folder:", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(500));
                foreach (string fileName in fileNames)
                {
                    GUILayout.BeginHorizontal();
                    var relativePath = GetRelativePath(folderPath, fileName);
                    bool isSelected = (relativePath == EditorPrefs.GetString(SelectedMap, ""));
                    bool newIsSelected = GUILayout.Toggle(isSelected, "", GUILayout.Width(20));

                    if (newIsSelected != isSelected)
                    {
                        if (newIsSelected)
                        {
                            EditorPrefs.SetString(SelectedMap, relativePath);
                        }
                        else
                        {
                            EditorPrefs.DeleteKey(SelectedMap);
                        }
                    }
                    GUILayout.Label(relativePath, GUILayout.Width(380));
                    if (GUILayout.Button("Save", GUILayout.Width(50)))
                    {
                        Save(fileName);
                        if (DeleteMapAfterSave)
                        {
                            MiscFunctions_RRT.DeleteAllRootGameObjects();
                        }
                    }
                    if (GUILayout.Button("Load", GUILayout.Width(50)))
                    {
                        Load(fileName);
                    }
                    if (GUILayout.Button("Copy Path", GUILayout.Width(80)))
                    {
                        EditorGUIUtility.systemCopyBuffer = relativePath;
                        Debug.Log("Copied relative path to clipboard: " + relativePath);
                    }
                    GUILayout.EndHorizontal();
                    Rect rect = GUILayoutUtility.GetRect(1, 1);
                    EditorGUI.DrawRect(rect, separatorColor);
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
        basePath = basePath.Replace("\\", "/");
        fullPath = fullPath.Replace("\\", "/");
        return fullPath.StartsWith(basePath) ? fullPath.Substring(basePath.Length + 1) : fullPath;
    }
    public List<MetaTileMap> SortObjectsByChildIndex(List<MetaTileMap> objects)
    {
	    objects.Sort((x, y) => y.transform.parent.GetSiblingIndex().CompareTo(x.transform.parent.GetSiblingIndex()));
	    return objects;
    }

    private void Load(string filePath)
    {
	    MapSaver.MapSaver.CodeClass.ThisCodeClass.Reset();
	    string data = "";
	    if (  string.IsNullOrEmpty(filePath) == false)
	    {
		    data = AccessFile.Load(filePath, FolderType.Maps);
	    }
	    else if ( string.IsNullOrEmpty(customJsonInput) == false)
	    {
		    data = customJsonInput;
	    }
	    else
	    {
		    return;
	    }

	    var mapData = JsonConvert.DeserializeObject<MapSaver.MapSaver.MapData>(data);
	    var Imnum = MapLoader.ServerLoadMap(Vector3.zero, Vector3.zero, mapData);
	    List<IEnumerator> previousLevels = new List<IEnumerator>();
	    bool loop = true;
	    while (loop && previousLevels.Count == 0)
	    {
		    if (Imnum.Current is IEnumerator)
		    {
			    previousLevels.Add(Imnum);
			    Imnum = (IEnumerator)Imnum.Current;
		    }
		    loop = Imnum.MoveNext();
		    if (!loop && previousLevels.Count > 0)
		    {
			    Imnum = previousLevels[^1];
			    previousLevels.RemoveAt(previousLevels.Count - 1);
			    loop = Imnum.MoveNext();
		    }
	    }
    }


    private void LoadFilesFromFolder()
    {
        fileNames = Directory.Exists(folderPath)
            ? Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".meta")).ToArray()
            : new string[0];
    }

    private void Save(string filePath)
    {
        try
        {
	        if (string.IsNullOrEmpty(customJsonInput))
	        {
		        var mapMatrices = Object.FindObjectsByType<MetaTileMap>(FindObjectsSortMode.None).ToList();
		        mapMatrices = SortObjectsByChildIndex(mapMatrices);
		        if (mapMatrices.Count == 0)
		        {
			        Loggy.Error($"No maps found for Save {filePath}");
			        return;
		        }
		        mapMatrices.Reverse();
		        JsonSerializerSettings settings = new JsonSerializerSettings
		        {
			        NullValueHandling = NullValueHandling.Ignore,
			        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
			        Formatting = Formatting.Indented
		        };
		        var map = MapSaver.MapSaver.SaveMap(mapMatrices, false, mapMatrices[0].name);
		        string jsonToSave = JsonConvert.SerializeObject(map, settings);
		        AccessFile.Save(filePath, jsonToSave, FolderType.Maps);
		        EditorUtility.DisplayDialog("Save Complete", $"Map saved successfully to {filePath}.", "OK");
	        }
	        else
	        {
		        string jsonToSave = customJsonInput;
		        AccessFile.Save(filePath, jsonToSave, FolderType.Maps);
		        EditorUtility.DisplayDialog("Save Complete", $"Map saved successfully to {filePath}.", "OK");
	        }




        }
        catch (Exception e)
        {
            Loggy.Error(e.ToString());
        }
    }
}
