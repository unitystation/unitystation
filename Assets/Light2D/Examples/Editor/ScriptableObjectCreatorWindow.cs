using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;


internal class ScriptableObjectCreatorWindow : EditorWindow
{
    private string _className = "";

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Scriptable Object Creator")]
    private static void Init()
    {
        // Get existing open window or if none, make a new one:
        var window = (ScriptableObjectCreatorWindow) EditorWindow.GetWindow(typeof (ScriptableObjectCreatorWindow));
    }

    private void OnGUI()
    {
        _className = EditorGUILayout.TextField("Class Name", _className);

        if (GUILayout.Button("Create"))
        {
            CreateAsset(_className);
        }
    }
    
    /// <summary>
    //	This makes it easy to create, name and place unique new ScriptableObject asset files.
    /// </summary>
    public static void CreateAsset(string type) 
    {
        var asset = ScriptableObject.CreateInstance(type);

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        Debug.Log(path + "/" + type + ".asset");

        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + type + ".asset");

        AssetDatabase.CreateAsset(asset, assetPathAndName);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}

