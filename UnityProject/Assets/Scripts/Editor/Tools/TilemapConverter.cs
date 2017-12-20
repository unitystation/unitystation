using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tilemaps.Scripts.Tiles;
using UnityEditor;
using UnityEngine;

namespace UnityStation.Tools
{
	public class TilemapConverter
	{
		private const string tilePath = "Assets/Tilemaps/Tiles/";
		private Dictionary<string, string> mapping;

		public TilemapConverter()
		{
			LoadMapping();
		}

		private void LoadMapping()
		{
			mapping = new Dictionary<string, string>();
			string[][] lines = File.ReadLines("Assets/Tilemaps/Mapping.csv").Select(a => a.Split(';')).ToArray();

			foreach (string[] line in lines)
			{
				string[] split = line[0].Split(',');
				mapping.Add(split[0], split[1]);
			}
		}

		public GenericTile DataToTile(UniTileData data)
		{
			string name = mapping.ContainsKey(data.OriginalSpriteName)
				? data.OriginalSpriteName
				: data.Name.Split('(')[0].Trim();

			if (!mapping.ContainsKey(name))
			{
				Debug.LogError("Missing tile for key: " + name);
				return null;
			}

			string assetPath = Path.Combine(tilePath, mapping[name] + ".asset");

			if (!File.Exists(assetPath))
			{
				string altAssetPath = Path.Combine(tilePath, mapping[name] + "_0.asset");
				if (File.Exists(altAssetPath))
				{
					assetPath = altAssetPath;
				}
				else
				{
					Debug.LogError("Missing tile at path: " + assetPath + " (key: " + name + " )");
					return null;
				}
			}

			return AssetDatabase.LoadAssetAtPath<GenericTile>(assetPath);
		}
	}
}