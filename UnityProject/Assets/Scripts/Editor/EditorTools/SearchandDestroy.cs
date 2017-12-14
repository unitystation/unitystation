using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SearchAndDestroy : EditorWindow
{
    private string componentName = "";
    private int editorMode, editorModeOld;

    private List<string> listResult;


    private readonly string[] modes =
        {"Search for component usage", "Search for missing components", "Remove from All prefabs"};

    private Vector2 scroll;
    private MonoScript targetComponent, lastChecked;

    [MenuItem("Tools/Components: Search and Destroy")]
    private static void Init()
    {
        var window = (SearchAndDestroy) GetWindow(typeof(SearchAndDestroy));
        window.Show();
        window.position = new Rect(50, 100, 600, 600);
    }

    private void OnGUI()
    {
        GUILayout.Space(4);
        var oldValue = GUI.skin.window.padding.bottom;
        GUI.skin.window.padding.bottom = -20;
        var windowRect = GUILayoutUtility.GetRect(1, 17);
        windowRect.x += 4;
        windowRect.width -= 7;
        editorMode = GUI.SelectionGrid(windowRect, editorMode, modes, 3, "Window");
        GUI.skin.window.padding.bottom = oldValue;

        if (editorModeOld != editorMode)
        {
            editorModeOld = editorMode;
            listResult = new List<string>();
            componentName = targetComponent == null ? "" : targetComponent.name;
            lastChecked = null;
        }

        switch (editorMode)
        {
            case 0:
                targetComponent = (MonoScript) EditorGUILayout.ObjectField(targetComponent, typeof(MonoScript), false);

                if (targetComponent != lastChecked)
                {
                    lastChecked = targetComponent;
                    componentName = targetComponent.name;
                    AssetDatabase.SaveAssets();
                    var targetPath = AssetDatabase.GetAssetPath(targetComponent);
                    var allPrefabs = GetAllPrefabs();
                    listResult = new List<string>();
                    var t = allPrefabs.Length;
                    var counter = 0;
                    foreach (var prefab in allPrefabs)
                    {
                        counter++;
                        EditorUtility.DisplayProgressBar(t + "/" + t + " Searching for Component...",
                            "prefab: " + counter, counter / (float) t);
                        string[] single = {prefab};
                        var dependencies = AssetDatabase.GetDependencies(single);
                        foreach (var dependedAsset in dependencies)
                        {
                            if (dependedAsset == targetPath)
                            {
                                listResult.Add(prefab);
                            }
                        }
                    }
                    EditorUtility.ClearProgressBar();
                }
                break;
            case 1:
                if (GUILayout.Button("Search!"))
                {
                    var allPrefabs = GetAllPrefabs();
                    listResult = new List<string>();
                    var t = allPrefabs.Length;
                    var counter = 0;
                    foreach (var prefab in allPrefabs)
                    {
                        counter++;
                        EditorUtility.DisplayProgressBar(t + "/" + t + " Searching for... nothing... ",
                            "prefab: " + counter, counter / (float) t);
                        var o = AssetDatabase.LoadMainAssetAtPath(prefab);
                        GameObject go;
                        try
                        {
                            go = (GameObject) o;
                            var components = go.GetComponentsInChildren<Component>(true);
                            foreach (var c in components)
                            {
                                if (c == null)
                                {
                                    listResult.Add(prefab);
                                }
                            }
                        }
                        catch
                        {
                            Debug.Log("For some reason, prefab " + prefab + " won't cast to GameObject");
                        }
                    }
                    EditorUtility.ClearProgressBar();
                }
                break;
            case 2:
                targetComponent = (MonoScript) EditorGUILayout.ObjectField(targetComponent, typeof(MonoScript), false);

                if (targetComponent != lastChecked)
                {
                    lastChecked = targetComponent;
                    componentName = targetComponent.name;
                    AssetDatabase.SaveAssets();
                    var targetPath = AssetDatabase.GetAssetPath(targetComponent);
                    var allPrefabs = GetAllPrefabs();
                    listResult = new List<string>();
                    var t = allPrefabs.Length;
                    var i = 0;
                    var counter = 0;
                    foreach (var prefab in allPrefabs)
                    {
                        counter++;
                        EditorUtility.DisplayProgressBar(counter + "/" + t + " Removing Component...",
                            "prefab: " + counter, counter / (float) t);
                        string[] single = {prefab};
                        var dependencies = AssetDatabase.GetDependencies(single);
                        foreach (var dependedAsset in dependencies)
                        {
                            if (dependedAsset == targetPath)
                            {
                                //			Debug.Log ("dependend: " + dependedAsset);
                                //			Debug.Log ("prefab: " + prefab);
                                //			Debug.Log ("target:" + componentName);
                                //			Debug.Log ("DETROYED");
                                //			var castPrefab = AssetDatabase.LoadAssetAtPath(prefab, (typeof(GameObject))) as GameObject;
                                //			var cast = AssetDatabase.LoadAssetAtPath(prefab, (typeof(GameObject))) as GameObject;
                                var cast = PrefabUtility.InstantiatePrefab(
                                    AssetDatabase.LoadAssetAtPath(prefab, typeof(GameObject))) as GameObject;

                                //			EditorUtility.SetDirty (castGO);
                                var component = cast.GetComponent(componentName);
                                DestroyImmediate(component, true);
                                //Debug.Log
                                //			PrefabUtility.ReplacePrefab(castGO, castPrefab, ReplacePrefabOptions.Default);
                                PrefabUtility.ReplacePrefab(cast, PrefabUtility.GetPrefabParent(cast),
                                    ReplacePrefabOptions.ConnectToPrefab);
                                DestroyImmediate(cast, true);
                                Debug.Log("Removed " + componentName + " From " + prefab);
                                i++;
                            }
                        }
                    }
                    Debug.Log("Removed components from" + i + " prefabs");
                    EditorUtility.ClearProgressBar();
                }
                break;
        }

        if (listResult != null)
        {
            if (listResult.Count == 0)
            {
                GUILayout.Label(editorMode == 0
                    ? (componentName == "" ? "Choose a component" : "No prefabs use component " + componentName)
                    : "No prefabs have missing components!\nClick Search to check again");
            }
            else
            {
                GUILayout.Label(editorMode == 0
                    ? "The following prefabs use component " + componentName + ":"
                    : "The following prefabs have missing components:");
                scroll = GUILayout.BeginScrollView(scroll);
                foreach (var s in listResult)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(s, GUILayout.Width(position.width / 2));
                    if (GUILayout.Button("Select", GUILayout.Width(position.width / 2 - 10)))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(s);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
        }
    }

    public static string[] GetAllPrefabs()
    {
        var temp = AssetDatabase.GetAllAssetPaths();
        var result = new List<string>();
        foreach (var s in temp)
        {
            if (s.Contains(".prefab"))
            {
                result.Add(s);
            }
        }
        return result.ToArray();
    }
}