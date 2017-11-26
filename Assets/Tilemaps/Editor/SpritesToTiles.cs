using System;
using System.Collections.Generic;
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
                if (entry.Key.Contains("floors.dmi"))
                {
                    var index = 0;
                    for(int i = 0; i < entry.Value.states.Count; i++)
                    {
                        var state = entry.Value.states[i];
//                        Debug.Log(state.unityName + " " + state.state + " " + state.frames + " " + state.dirs);
                
                        var assetName = "floors_" + index;
                        
                        var dmiIndex = int.Parse(state.unityName.Replace("floors_", ""));

                        if (dmiIndex != index)
                        {
                            Debug.Log(state.state + " " + state.unityName);
                            index = dmiIndex;
                        }
                        
                        index += Mathf.Max(state.frames, state.dirs);


//                        TileBuilder.CreateTile<SimpleTile>(LayerType.Floors, state.state);
                    }

                    break;
                }
            }
        }
    }
}