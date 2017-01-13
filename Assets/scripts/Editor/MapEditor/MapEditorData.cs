using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MapEditorData {
    private static Dictionary<string, GameObject[]> prefabs = new Dictionary<string, GameObject[]>();
    private static Dictionary<string, Texture2D[]> textures = new Dictionary<string, Texture2D[]>();

    public static Dictionary<string, GameObject[]> Prefabs {
        get {
            return prefabs;
        }
    }

    public static Dictionary<string, Texture2D[]> Textures {
        get {
            return textures;
        }
    }

    public static void Clear() {
        prefabs.Clear();
        textures.Clear();
    }

    public static void Load(string prefabPath) {
        var prefabs = LoadPrefabs(prefabPath);
        var textures = LoadTextures(prefabs);

        MapEditorData.prefabs[prefabPath] = prefabs;
        MapEditorData.textures[prefabPath] = textures;
    }

    private static GameObject[] LoadPrefabs(string prefabPath) {
        List<GameObject> list = new List<GameObject>();
        string[] prefabs = Directory.GetFiles(Application.dataPath + "/Prefabs/" + prefabPath, "*.prefab", SearchOption.TopDirectoryOnly);
        foreach(string prefabFile in prefabs) {
            string assetPath = "Assets" + prefabFile.Replace(Application.dataPath, "").Replace('\\', '/');
            list.Add(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
        }

        return list.ToArray();
    }

    private static Texture2D[] LoadTextures(GameObject[] prefabs) {

        var content = new List<Texture2D>();

        foreach(var p in prefabs) {
            content.Add(AssetPreview.GetAssetPreview(p));
        }

        return content.ToArray();
    }
}
