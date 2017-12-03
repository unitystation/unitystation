using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MapEditor {

    public class GridData {
        public Texture2D[] Textures { get; private set; }
        public GameObject[] Prefabs { get; private set; }

        static GridData() {
            AssetPreview.SetPreviewTextureCacheSize(1000);
        }

        public GridData(string prefabPath) {
            Load(prefabPath);
        }

        private void Load(string prefabPath) {
            Prefabs = LoadPrefabs(prefabPath);
            Textures = LoadTextures(Prefabs);
        }

        private GameObject[] LoadPrefabs(string prefabPath) {
            List<GameObject> list = new List<GameObject>();

            string dataPath = Application.dataPath + "/Prefabs/" + prefabPath;
            if(Directory.Exists(dataPath)) {
                string[] prefabs = Directory.GetFiles(dataPath, "*.prefab", SearchOption.TopDirectoryOnly);
                foreach(string prefabFile in prefabs) {
                    string assetPath = "Assets" + prefabFile.Replace(Application.dataPath, "").Replace('\\', '/');
                    list.Add(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
                }
            }
            return list.ToArray();
        }

        private Texture2D[] LoadTextures(GameObject[] prefabs) {

            var content = new List<Texture2D>();

            foreach(var p in prefabs) {
                if (p.GetComponent<Matrix.RegisterTile>() != null|| (p.GetComponent<EditModeControl>() != null)) {
                Texture2D texture;
                do {
                    texture = AssetPreview.GetAssetPreview(p);
                } while(!texture);

                content.Add(texture);
                }
                
            }

            return content.ToArray();
        }
    }
}
