using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework.Constraints;
using Sprites;
using Tilemaps.Scripts.Tiles;
using UnityEditor;
using UnityEngine;

namespace UnityStation.Tools
{
    public static class TilemapConverter
    {
        private static Dictionary<string, string> mapping = null;

        private const string tilePath = "Assets/Tilemaps/Tiles/";

        public static void LoadMapping()
        {
            mapping = new Dictionary<string, string>();
            var lines = File.ReadLines("Assets/Tilemaps/Mapping.csv").Select(a => a.Split(';')).ToArray();

            foreach (var line in lines)
            {
                var split = line[0].Split(',');
                mapping.Add(split[0], split[1]);
            }
        }
        
        public static GenericTile DataToTile(UniTileData data)
        {
            var name = data.Name.Split('(')[0].Trim();
            if (!mapping.ContainsKey(name))
            {
                Debug.LogError("Missing tile for key: " + name);
                return null;
            }
            
            var assetPath = Path.Combine(tilePath, mapping[name] + ".asset");

            if (!File.Exists(assetPath))
            {
                var altAssetPath = Path.Combine(tilePath, mapping[name] + "_0.asset");
                if (File.Exists(altAssetPath))
                {
                    assetPath = altAssetPath;
                }
                else
                {
                    Debug.LogError("Missing tile at path: " + assetPath);
                    return null;
                }
            }
            
            return AssetDatabase.LoadAssetAtPath<GenericTile>(assetPath);
        }
    }
}