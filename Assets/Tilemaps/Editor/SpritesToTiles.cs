using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tilemaps.Editor.Utils;
using Tilemaps.Scripts.Tiles;
using UnityEditor;
using UnityEngine;

namespace Tilemaps.Editor
{
    public class SpritesToTiles : MonoBehaviour
    {
        private static string tilesPath = "Assets/Tilemaps/Tiles";
    
        [MenuItem("Assets/Sprites/Generate Tiles", false, 1000)]
        public static void ImportObjects()
        {
            foreach (var obj in Selection.objects)
            {
                LoadTiles(AssetDatabase.GetAssetPath(obj));
            }
        }

        private static void LoadTiles(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            
            foreach (KeyValuePair<string, DmiIcon> entry in DmiIconData.Data)
            {
                if (entry.Key.Contains("floors.dmi")) // TODO only supports floors right now
                {
                    var folderPath = Path.Combine(tilesPath, assets[0].name);
                    
                    for(int i = 0; i < entry.Value.states.Count; i++)
                    {
                        var state = entry.Value.states[i];
                        
                        var dmiIndex = int.Parse(state.unityName.Replace("floors_", ""));

                        var tileCount = state.frames * state.dirs;
                        
                        for (int e = 0; e < state.frames * state.dirs; e++)
                        {
                            var tileName = state.state + (tileCount > 1? "_" + e: "");
                            
                            var tile = TileBuilder.CreateTile<SimpleTile>(LayerType.Floors);
                            tile.sprite = assets[dmiIndex + e + 1] as Sprite;
                            tile.LayerType = LayerType.Floors;

                            if (tileName.Length == 0)
                            {
                                tileName = "FloorTile";
                            }
                            
                            TileBuilder.CreateAsset(tile, tileName, folderPath);
                        }
                    }

                    break;
                }
            }
        }
    }
}