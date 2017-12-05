using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class SearchAndDestroy : EditorWindow
{
    [MenuItem("Tools/Components: Search and Destroy")]
    static void Init()
    {
        SearchAndDestroy window = (SearchAndDestroy)EditorWindow.GetWindow(typeof(SearchAndDestroy));
        window.Show();
        window.position = new Rect(50, 100, 600, 600);
    }


    string[] modes = new string[] { "Search for component usage", "Search for missing components", "Remove from All prefabs" };

    List<string> listResult;
    int editorMode, editorModeOld;
    MonoScript targetComponent, lastChecked;
    string componentName = "";
    Vector2 scroll;

    void OnGUI()
    {
        GUILayout.Space(4);
        int oldValue = GUI.skin.window.padding.bottom;
        GUI.skin.window.padding.bottom = -20;
        Rect windowRect = GUILayoutUtility.GetRect(1, 17);
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
                targetComponent = (MonoScript)EditorGUILayout.ObjectField(targetComponent, typeof(MonoScript), false);

                if (targetComponent != lastChecked)
                {
                    lastChecked = targetComponent;
                    componentName = targetComponent.name;
                    AssetDatabase.SaveAssets();
                    string targetPath = AssetDatabase.GetAssetPath(targetComponent);
                    string[] allPrefabs = GetAllPrefabs();
                    listResult = new List<string>();
                    int t = allPrefabs.Length;
                    int counter = 0;
                    foreach (string prefab in allPrefabs)
                    {
                        counter++;
                        EditorUtility.DisplayProgressBar(t.ToString() + "/" + t + " Searching for Component...", "prefab: " + counter, (float)counter / (float)t);
                        string[] single = new string[] { prefab };
                        string[] dependencies = AssetDatabase.GetDependencies(single);
                        foreach (string dependedAsset in dependencies)
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
                    string[] allPrefabs = GetAllPrefabs();
                    listResult = new List<string>();
                    int t = allPrefabs.Length;
                    int counter = 0;
                    foreach (string prefab in allPrefabs)
                    {
                        counter++;
                        EditorUtility.DisplayProgressBar(t.ToString() + "/" + t + " Searching for... nothing... ", "prefab: " + counter, (float)counter / (float)t);
                        UnityEngine.Object o = AssetDatabase.LoadMainAssetAtPath(prefab);
                        GameObject go;
                        try
                        {
                            go = (GameObject)o;
                            Component[] components = go.GetComponentsInChildren<Component>(true);
                            foreach (Component c in components)
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
                targetComponent = (MonoScript)EditorGUILayout.ObjectField(targetComponent, typeof(MonoScript), false);

                if (targetComponent != lastChecked)
                {
                    lastChecked = targetComponent;
                    componentName = targetComponent.name;
                    AssetDatabase.SaveAssets();
                    string targetPath = AssetDatabase.GetAssetPath(targetComponent);
                    string[] allPrefabs = GetAllPrefabs();
                    listResult = new List<string>();
                    int t = allPrefabs.Length;
                    int i = 0;
                    int counter = 0;
                    foreach (string prefab in allPrefabs)
                    {
                        counter++;
                        EditorUtility.DisplayProgressBar(counter.ToString() + "/" + t + " Removing Component...", "prefab: " + counter, (float)counter / (float)t);
                        string[] single = new string[] { prefab };
                        string[] dependencies = AssetDatabase.GetDependencies(single);
                        foreach (string dependedAsset in dependencies)
                        {
                            if (dependedAsset == targetPath)
                            {
                                //			Debug.Log ("dependend: " + dependedAsset);
                                //			Debug.Log ("prefab: " + prefab);
                                //			Debug.Log ("target:" + componentName);
                                //			Debug.Log ("DETROYED");
                                //			var castPrefab = AssetDatabase.LoadAssetAtPath(prefab, (typeof(GameObject))) as GameObject;
                                //			var cast = AssetDatabase.LoadAssetAtPath(prefab, (typeof(GameObject))) as GameObject;
                                var cast = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath(prefab, typeof(GameObject))) as GameObject;

                                //			EditorUtility.SetDirty (castGO);
                                var component = cast.GetComponent(componentName) as Component;
                                DestroyImmediate(component, true);
                                //Debug.Log
                                //			PrefabUtility.ReplacePrefab(castGO, castPrefab, ReplacePrefabOptions.Default);
                                PrefabUtility.ReplacePrefab(cast, PrefabUtility.GetPrefabParent(cast), ReplacePrefabOptions.ConnectToPrefab);
                                GameObject.DestroyImmediate(cast, true);
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
                GUILayout.Label(editorMode == 0 ? (componentName == "" ? "Choose a component" : "No prefabs use component " + componentName) : ("No prefabs have missing components!\nClick Search to check again"));
            }
            else
            {
                GUILayout.Label(editorMode == 0 ? ("The following prefabs use component " + componentName + ":") : ("The following prefabs have missing components:"));
                scroll = GUILayout.BeginScrollView(scroll);
                foreach (string s in listResult)
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
        string[] temp = AssetDatabase.GetAllAssetPaths();
        List<string> result = new List<string>();
        foreach (string s in temp)
        {
            if (s.Contains(".prefab")) result.Add(s);
        }
        return result.ToArray();
    }
}