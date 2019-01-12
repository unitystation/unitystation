using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TilePaths
{
	private static Dictionary<TileType, string> paths = new Dictionary<TileType, string>()
	{
		{TileType.Base, "Tiles/Base"},
		{TileType.Floor, "Tiles/Floors"},
		{TileType.Table, "Tiles/Tables"},
		{TileType.Window, "Tiles/Windows"},
		{TileType.Wall, "Tiles/Walls"},
		{TileType.Grill, "Tiles/Objects"}, // TODO remove
		{TileType.Object, "Tiles/Objects"},
		{TileType.Damaged, "Tiles/Damaged"},
		{TileType.Effects, "Tiles/Effects"}
	};

	public static string Get(TileType type)
	{
		return paths.ContainsKey(type) ? paths[type] : null;
	}
}

public class TileDictionary : Dictionary<Tuple<TileType, string>, LayerTile>
{
	public LayerTile this[TileType type, string name]
	{
		get { return this[new Tuple<TileType, string>(type, name)]; }
		set { this[new Tuple<TileType, string>(type, name)] = value; }
	}
}

public class TileManager : MonoBehaviour
{
	private static TileDictionary tiles = new TileDictionary();

	private void Start()
	{
		LoadAllTiles();
	}

	private static void LoadAllTiles()
	{
		foreach (TileType tileType in Enum.GetValues(typeof(TileType)))
		{
			string path = TilePaths.Get(tileType);

			if (path != null)
			{
				LayerTile[] loadedTiles = Resources.LoadAll<LayerTile>(path);
				loadedTiles.ToList().ForEach(x => tiles[tileType, x.name] = x);
			}
		}
	}

	public static LayerTile GetTile(TileType tileType, string key)
	{
		return tiles[tileType, key];
	}
}