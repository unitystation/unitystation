using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MapEditor {

    public class MapEditorData {

        static MapEditorData() {
            AssetPreview.SetPreviewTextureCacheSize(1000);

            Prefabs = new Dictionary<string, GameObject[]>();
            Textures = new Dictionary<string, Texture2D[]>();
        }

        public static Dictionary<string, GameObject[]> Prefabs { get; private set; }
        public static Dictionary<string, Texture2D[]> Textures { get; private set; }

        public static GameObject[] CurrentPrefabs {
            get {
                return Prefabs[MapEditorMap.CurrentSubSectionName];
            }
        }

        public static Texture2D[] CurrentTextures {
            get {
                return Textures[MapEditorMap.CurrentSubSectionName];
            }
        }

        public static void Clear() {
            Prefabs.Clear();
            Textures.Clear();
        }

        public static void LoadPrefabs() {
            foreach(var s in MapEditorMap.SubSectionNames) {
                Load(s);
            }
        }

        public static void Load(string prefabPath) {
            var prefabs = LoadPrefabs(prefabPath);
            var textures = LoadTextures(prefabs);

            Prefabs[prefabPath] = prefabs;
            Textures[prefabPath] = textures;
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
                Texture2D texture;
                do {
                    texture = AssetPreview.GetAssetPreview(p);
                } while(!texture);

                content.Add(texture);
            }

            return content.ToArray();
        }
    }
}