using System.IO;
using Tilemaps.Scripts.Tiles;
using UnityEditor;
using UnityEngine;

namespace Tilemaps.Editor.Utils
{
    public static class TileBuilder
    {
        public static void CreateTile<T>(LayerType layer, string tileName) where T : LayerTile
        {
            CreateAsset(CreateTile<T>(layer), tileName);
        }

        public static T CreateTile<T>(LayerType layer) where T : LayerTile
        {
            var tile = ScriptableObject.CreateInstance<T>();
            tile.LayerType = layer;
            return tile;
        }

        public static void CreateAsset(Object asset, string tileName)
        {
            var assetPath = Path.Combine(GetPath(), tileName + ".asset");
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        private static string GetPath()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(path), "");
            }

            return path;
        }
    }
}