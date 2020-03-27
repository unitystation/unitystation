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
		{TileType.WindowDamaged, "Tiles/WindowDamage"},
		{TileType.Effects, "Tiles/Effects"}
	};

	public static string Get(TileType type)
	{
		return paths.ContainsKey(type) ? paths[type] : null;
	}
}

[Serializable]
public class TilePathEntry
{
	public string path;
	public TileType tileType;
	public List<LayerTile> layerTiles = new List<LayerTile>();
}

public class TileManager : MonoBehaviour
{
	private static TileManager tileManager;

	public static TileManager Instance
	{
		get
		{
			if (tileManager == null)
			{
				tileManager = FindObjectOfType<TileManager>();
			}

			return tileManager;
		}
	}

	private Dictionary<TileType, Dictionary<string, LayerTile>> tiles = new Dictionary<TileType, Dictionary<string, LayerTile>>();
	private bool initialized;

	[SerializeField] private List<TilePathEntry> layerTileCollections = new List<TilePathEntry>();

	private void Start()
	{
#if UNITY_EDITOR
		CacheAllAssets();
#endif
		LoadAllTiles();
	}

	[ContextMenu("Cache All Assets")]
	public bool CacheAllAssets()
	{
		layerTileCollections.Clear();
		foreach (TileType tileType in Enum.GetValues(typeof(TileType)))
		{
			string path = TilePaths.Get(tileType);

			if (path != null)
			{
				layerTileCollections.Add(new TilePathEntry
				{
					path = path,
					tileType = tileType,
					layerTiles = Resources.LoadAll<LayerTile>(path).ToList()
				});
			}
		}

		return true;
	}

	private void LoadAllTiles()
	{
		foreach (var type in layerTileCollections)
		{
			if (!tiles.ContainsKey(type.tileType))
			{
				tiles.Add(type.tileType, new Dictionary<string, LayerTile>());
			}

			foreach (var t in type.layerTiles)
			{
				if (t.TileType == type.tileType)
				{
					if (!tiles[type.tileType].ContainsKey(t.name))
					{
						tiles[type.tileType].Add(t.name, t);
					}
				}
			}
		}

		initialized = true;
	}

	public static LayerTile GetTile(TileType tileType, string key)
	{
		if (!Instance.initialized) Instance.LoadAllTiles();
		return Instance.tiles[tileType][key];
	}
}