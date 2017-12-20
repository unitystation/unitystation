using System.Collections.Generic;
using System.IO;
using Tilemaps.Editor.Utils;
using Tilemaps.Scripts.Tiles;
using UnityEditor;
using UnityEngine;

namespace Tilemaps.Editor
{
	public class SpritesToTiles : MonoBehaviour
	{
		private const string tilesPath = "Assets/Tilemaps/Tiles";

		[MenuItem("Assets/Sprites/Generate Tiles", false, 1000)]
		public static void ImportObjects()
		{
			foreach (Object obj in Selection.objects)
			{
				LoadTiles(AssetDatabase.GetAssetPath(obj));
			}
		}

		private static void LoadTiles(string path)
		{
			Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

			foreach (KeyValuePair<string, DmiIcon> entry in DmiIconData.Data)
			{
				if (entry.Key.Contains("floors.dmi")) // TODO only supports floors right now
				{
					string folderPath = Path.Combine(tilesPath, assets[0].name);

					foreach (DmiState state in entry.Value.states)
					{
						int dmiIndex = int.Parse(state.unityName.Replace("floors_", ""));

						int tileCount = state.frames * state.dirs;

						for (int e = 0; e < state.frames * state.dirs; e++)
						{
							string tileName = state.state + (tileCount > 1 ? "_" + e : "");

							SimpleTile tile = TileBuilder.CreateTile<SimpleTile>(LayerType.Floors);
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