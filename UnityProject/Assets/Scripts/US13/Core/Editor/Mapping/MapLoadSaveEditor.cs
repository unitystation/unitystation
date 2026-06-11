using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.IO;
using System.Linq;
using Logs;
using Newtonsoft.Json;
using US13.Managers.SubSceneManager;
using US13.MapSaver;
using US13.Tilemaps.Behaviours.Layers;
using Util;
using Object = UnityEngine.Object;

public class FileSelectorWindow : EditorWindow
{
    private string mapsRoot = "";
    private string roomsRoot = "";
    private string[] mapFiles = Array.Empty<string>();
    private string[] roomFiles = Array.Empty<string>();
    private string searchFilter = "";

    // If loaded from file, stores the active file path for easy saving
    private string activeFilePath = "";

    // For the paste json field in advanced accordian 
    private string pasteJson = "";

    private bool showAdvanced = false;
    private bool showMaps = false;
    private bool showRooms = false;
    private bool wasFiltering = false;
    private Vector2 panelScroll = Vector2.zero;
    private readonly Color separatorColor = Color.gray;
    private GUIStyle cardStyle;
    private GUIStyle listStyle;
    private SearchField searchField;

    private const string SelectedMap = "SelectedMap";
    private static bool DeleteMapAfterSave = false;
    private static bool reCentrePositions = false;

    [MenuItem("Mapping/𓃡𓃡 Map Loader Saver Selector 𓃡𓃡")]
    public static void ShowWindow()
    {
        GetWindow<FileSelectorWindow>("𓃡𓃡 Map Loader Saver Selector 𓃡𓃡");
    }

    private void OnEnable()
    {
        mapsRoot = Path.Combine(Application.dataPath, "StreamingAssets/Maps");
        roomsRoot = Path.Combine(Application.dataPath, "StreamingAssets/Rooms");
        if (EditorPrefs.HasKey(SelectedMap))
        {
            SubSceneManager.AdminForcedMainStation = EditorPrefs.GetString(SelectedMap);
        }
        RefreshFileLists();
    }

    private void OnGUI()
    {
        if (cardStyle == null)
        {
            cardStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(12, 12, 12, 12) };
        }
        if (listStyle == null)
        {
            var listTint = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            listTint.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.15f));
            listTint.Apply();
            listStyle = new GUIStyle(GUIStyle.none) { padding = new RectOffset(6, 6, 6, 6) };
            listStyle.normal.background = listTint;
        }

        panelScroll = EditorGUILayout.BeginScrollView(panelScroll);

        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        GUILayout.BeginVertical();
        GUILayout.Space(20);

        GUILayout.BeginVertical(cardStyle);
        // Active file
        GUILayout.Label("Active file", EditorStyles.boldLabel);
        GUILayout.Label(string.IsNullOrEmpty(activeFilePath)
            ? "(none - load one below)"
            : MakeDisplayLabel(activeFilePath));

        GUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(activeFilePath)))
        {
            if (GUILayout.Button("Save", GUILayout.Width(80)))
            {
                SaveToPath(activeFilePath);
            }
        }
        if (GUILayout.Button("Save As...", GUILayout.Width(90)))
        {
            string startDir = Directory.Exists(mapsRoot) ? mapsRoot : roomsRoot;
            string path = EditorUtility.SaveFilePanel("Save Map/Room As", startDir, "map.json", "json");
            if (string.IsNullOrEmpty(path) == false)
            {
                SaveToPath(path);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        if (GUILayout.Button("Clear Scene", GUILayout.Width(110)))
        {
            if (EditorUtility.DisplayDialog("Clear scene?",
                "This deletes every root object in the scene. Continue?", "Clear", "Cancel"))
            {
                MiscFunctions_RRT.DeleteAllRootGameObjects();
                activeFilePath = "";
            }
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical(cardStyle);
        // Load a map or room
        GUILayout.BeginHorizontal();
        GUILayout.Label("Load a map or room", EditorStyles.boldLabel);
        if (GUILayout.Button("Browse...", GUILayout.Width(90)))
        {
            string startDir = Directory.Exists(roomsRoot) ? roomsRoot : mapsRoot;
            string path = EditorUtility.OpenFilePanel("Load Map/Room JSON", startDir, "json");
            if (string.IsNullOrEmpty(path) == false)
            {
                LoadFile(path);
            }
        }
        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
        {
            RefreshFileLists();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Filter:", GUILayout.Width(40));
        searchField ??= new SearchField();
        Rect filterRect = GUILayoutUtility.GetRect(250, 18);
        searchFilter = searchField.OnGUI(filterRect, searchFilter);
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical(listStyle);
        var shownMaps = FilterFiles(mapFiles);
        var shownRooms = FilterFiles(roomFiles);
        bool filtering = string.IsNullOrWhiteSpace(searchFilter) == false;
        if (filtering && wasFiltering == false)
        {
            showMaps = true;
            showRooms = true;
        }
        wasFiltering = filtering;

        showMaps = EditorGUILayout.Foldout(showMaps, $"Maps ({shownMaps.Length})", true);
        if (showMaps)
        {
            EditorGUILayout.HelpBox("To test a map, tick the checkbox and press play in the editor.", MessageType.None);
            DrawRows(shownMaps, mapsRoot, showMainStationToggle: true);
        }

        showRooms = EditorGUILayout.Foldout(showRooms, $"Rooms ({shownRooms.Length})", true);
        if (showRooms)
        {
            DrawRows(shownRooms, roomsRoot, showMainStationToggle: false);
        }
        GUILayout.EndVertical();
        GUILayout.EndVertical();

        GUILayout.Space(8);

        GUILayout.BeginVertical(cardStyle);
        // Advanced
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);
        if (showAdvanced)
        {
            DeleteMapAfterSave = GUILayout.Toggle(DeleteMapAfterSave, "Delete map from scene after save");
            reCentrePositions = GUILayout.Toggle(reCentrePositions,
                "Recentre positions (causes git conflicts if others are editing the map)");

            GUILayout.Space(6);
            GUILayout.Label("Paste JSON", EditorStyles.boldLabel);
            pasteJson = EditorGUILayout.TextArea(pasteJson, GUILayout.Height(100));
            if (GUILayout.Button("Load From Text"))
            {
                if (string.IsNullOrEmpty(pasteJson))
                {
                    EditorUtility.DisplayDialog("Empty", "Paste some JSON first.", "OK");
                }
                else
                {
                    LoadFromJson(pasteJson, null);
                }
            }
        }
        GUILayout.EndVertical();

        GUILayout.EndVertical();
        GUILayout.Space(20);
        GUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private void DrawRows(string[] files, string root, bool showMainStationToggle)
    {
        if (files.Length == 0)
        {
            GUILayout.Label("   (none found)", EditorStyles.miniLabel);
            return;
        }

        string rootSlash = root.Replace("\\", "/");
        foreach (string abs in files)
        {
            GUILayout.Space(3);
            GUILayout.BeginHorizontal();

            if (showMainStationToggle)
            {
                string rel = abs.Replace("\\", "/");
                rel = rel.StartsWith(rootSlash) ? rel.Substring(rootSlash.Length + 1) : rel;
                bool isSelected = rel == EditorPrefs.GetString(SelectedMap, "");
                bool newSelected = GUILayout.Toggle(isSelected, "", GUILayout.Width(18));
                if (newSelected != isSelected)
                {
                    if (newSelected)
                    {
                        EditorPrefs.SetString(SelectedMap, rel);
                    }
                    else
                    {
                        EditorPrefs.DeleteKey(SelectedMap);
                    }
                    SubSceneManager.AdminForcedMainStation = EditorPrefs.GetString(SelectedMap);
                }
            }
            else
            {
                GUILayout.Space(22);
            }

            GUILayout.Label(MakeDisplayLabel(abs), GUILayout.Width(360));
            if (GUILayout.Button("Load", GUILayout.Width(50)))
            {
                LoadFile(abs);
            }
            if (GUILayout.Button("Save", GUILayout.Width(50)))
            {
                SaveToPath(abs);
            }
            if (GUILayout.Button("Copy Path", GUILayout.Width(80)))
            {
                string label = MakeDisplayLabel(abs);
                EditorGUIUtility.systemCopyBuffer = label;
                Debug.Log("Copied path: " + label);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            Rect rect = GUILayoutUtility.GetRect(1, 1);
            EditorGUI.DrawRect(rect, separatorColor);
        }
    }

    private void LoadFile(string absPath)
    {
        LoadFromJson(File.ReadAllText(absPath), absPath);
    }

    private void LoadFromJson(string json, string sourcePathOrNull)
    {
        if (string.IsNullOrEmpty(json))
        {
            EditorUtility.DisplayDialog("Nothing to load", "The JSON was empty.", "OK");
            return;
        }

        try
        {
            MapSaver.CodeClass.ThisCodeClass.Reset();
            var matricesBeforeLoad = new HashSet<MetaTileMap>(Object.FindObjectsByType<MetaTileMap>(FindObjectsSortMode.None));
            var mapData = JsonConvert.DeserializeObject<MapSaver.MapData>(json);
            RunCoroutineInEditor(MapLoader.ServerLoadMap(Vector3.zero, Vector3.zero, mapData));
            FocusSceneViewOnLoaded(matricesBeforeLoad);
            activeFilePath = sourcePathOrNull ?? "";
        }
        catch (Exception e)
        {
            Loggy.Error($"[MapLoadSave] Load failed: {e}");
            EditorUtility.DisplayDialog("Load failed",
                $"{e.Message}\n\nSee the Console for the full stack trace.",
                "OK");
        }
    }

    // Play mode ticks coroutines for us; the editor doesn't, so we run this one to completion ourselves.
    private static void RunCoroutineInEditor(IEnumerator coroutine)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(coroutine);
        while (stack.Count > 0)
        {
            IEnumerator top = stack.Peek();
            if (top.MoveNext() == false)
            {
                stack.Pop();
                continue;
            }
            if (top.Current is IEnumerator nested)
            {
                stack.Push(nested);
            }
        }
    }

    private void FocusSceneViewOnLoaded(HashSet<MetaTileMap> matricesBeforeLoad)
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null) return;

        var newMatrices = Object.FindObjectsByType<MetaTileMap>(FindObjectsSortMode.None)
            .Where(m => matricesBeforeLoad.Contains(m) == false)
            .Select(m => (Object)m.gameObject)
            .ToArray();
        if (newMatrices.Length == 0) return;

        Selection.objects = newMatrices;
        view.FrameSelected();
    }

    private string SerializeScene()
    {
        var mapMatrices = Object.FindObjectsByType<MetaTileMap>(FindObjectsSortMode.None).ToList();
        if (mapMatrices.Count == 0)
        {
            EditorUtility.DisplayDialog("Nothing to save", "No matrices found in the scene. Load a map/room first.", "OK");
            return null;
        }
        mapMatrices = SortMatricesForSave(mapMatrices);
        mapMatrices.Reverse();
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
            Formatting = Formatting.Indented
        };
        var map = MapSaver.SaveMap(mapMatrices, false, mapMatrices[0].name, ReadjustCentre: reCentrePositions);
        return JsonConvert.SerializeObject(map, settings);
    }

    private bool SaveToPath(string absPath)
    {
        try
        {
            string json = SerializeScene();
            if (json == null) return false;
            File.WriteAllText(absPath, json);
            EditorUtility.DisplayDialog("Save Complete", $"Saved to {MakeDisplayLabel(absPath)}", "OK");

            if (DeleteMapAfterSave)
            {
                MiscFunctions_RRT.DeleteAllRootGameObjects();
                activeFilePath = "";
            }
            else
            {
                activeFilePath = absPath;
            }
            return true;
        }
        catch (Exception e)
        {
            Loggy.Error($"[MapLoadSave] Save failed: {e}");
            EditorUtility.DisplayDialog("Save failed", e.Message, "OK");
            return false;
        }
    }

    private void RefreshFileLists()
    {
        mapFiles = ListJsonFiles(mapsRoot);
        roomFiles = ListJsonFiles(roomsRoot);
    }

    private static string[] ListJsonFiles(string root)
    {
        return Directory.Exists(root)
            ? Directory.GetFiles(root, "*.json", SearchOption.AllDirectories)
            : Array.Empty<string>();
    }

    private string[] FilterFiles(string[] files)
    {
        if (string.IsNullOrWhiteSpace(searchFilter)) return files;
        return files.Where(f => MakeDisplayLabel(f).IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
    }

    private string MakeDisplayLabel(string absPath)
    {
        string path = absPath.Replace("\\", "/");
        string maps = mapsRoot.Replace("\\", "/");
        string rooms = roomsRoot.Replace("\\", "/");
        if (path.StartsWith(maps)) return "Maps/" + path.Substring(maps.Length + 1);
        if (path.StartsWith(rooms)) return "Rooms/" + path.Substring(rooms.Length + 1);
        return absPath;
    }

    public List<MetaTileMap> SortMatricesForSave(List<MetaTileMap> matrices)
    {
        matrices.Sort((x, y) => y.transform.parent.GetSiblingIndex().CompareTo(x.transform.parent.GetSiblingIndex()));
        return matrices;
    }
}
