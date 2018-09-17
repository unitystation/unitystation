using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilesManager : MonoBehaviour
{
	private static TilesManager tilesManager;

	public static TilesManager Instance
	{
		get
		{
			if (tilesManager == null)
			{
				tilesManager = FindObjectOfType<TilesManager>();
			}
			return tilesManager;
		}
	}

	private Dictionary<string, TileBase> floorAssets = new Dictionary<string, TileBase>();
	private Dictionary<string, TileBase> wallAssets = new Dictionary<string, TileBase>();
	private Dictionary<string, TileBase> windowAssets = new Dictionary<string, TileBase>();
	private Dictionary<string, TileBase> objectAssets = new Dictionary<string, TileBase>();

	public static Dictionary<string, TileBase> FloorAssets => Instance.floorAssets;
	public static Dictionary<string, TileBase> WallAssets => Instance.wallAssets;
	public static Dictionary<string, TileBase> WindowAssets => Instance.windowAssets;
	public static Dictionary<string, TileBase> ObjectAssets => Instance.objectAssets;

	void Start()
	{
		LoadAllTileAssets();
	}

	void LoadAllTileAssets()
	{
		TileBase[] floorTiles = Resources.LoadAll<TileBase>("Tiles/Floors");
		for (int i = 0; i < floorTiles.Length; i++)
		{
			floorAssets.Add(floorTiles[i].name, floorTiles[i]);
		}

		TileBase[] windowTiles = Resources.LoadAll<TileBase>("Tiles/Windows");
		for (int i = 0; i < windowTiles.Length; i++)
		{
			windowAssets.Add(windowTiles[i].name, windowTiles[i]);
		}

		TileBase[] wallTiles = Resources.LoadAll<TileBase>("Tiles/Walls");
		for (int i = 0; i < wallTiles.Length; i++)
		{
			wallAssets.Add(wallTiles[i].name, wallTiles[i]);
		}

		TileBase[] tableTiles = Resources.LoadAll<TileBase>("Tiles/Tables");
		for (int i = 0; i < tableTiles.Length; i++)
		{
			objectAssets.Add(tableTiles[i].name, tableTiles[i]);
		}

		//FIXME: Do a recursive search through all sub folders in Objects:

		TileBase[] grillTiles = Resources.LoadAll<TileBase>("Tiles/Objects/Grills");
		for (int i = 0; i < grillTiles.Length; i++)
		{
			objectAssets.Add(grillTiles[i].name, grillTiles[i]);
		}

		TileBase[] windowDmgTiles = Resources.LoadAll<TileBase>("Tiles/WindowDamage");
		for (int i = 0; i < windowDmgTiles.Length; i++)
		{
			windowAssets.Add(windowDmgTiles[i].name, windowDmgTiles[i]);
		}
	}

	public static TileBase GetTile(TileChangeLayer layer, string tileKey)
	{
		switch (layer)
		{
			case TileChangeLayer.Base:
			case TileChangeLayer.Floor:
				if (FloorAssets.ContainsKey(tileKey))
				{
					return FloorAssets[tileKey];
				}
				break;
			case TileChangeLayer.Object:
			case TileChangeLayer.BrokenGrill:
				if (ObjectAssets.ContainsKey(tileKey))
				{
					return ObjectAssets[tileKey];
				}
				break;
			case TileChangeLayer.Wall:
				if (WallAssets.ContainsKey(tileKey))
				{
					return WallAssets[tileKey];
				}
				break;
			case TileChangeLayer.Window:
			case TileChangeLayer.WindowDamage:
				if (WindowAssets.ContainsKey(tileKey))
				{
					return WindowAssets[tileKey];
				}
				break;
		}
		return null;
	}

	public static bool ValidateTileKey(TileChangeLayer layer, string tileKey)
	{
		if (string.IsNullOrEmpty(tileKey))
		{
			return true;
		}

		switch (layer)
		{
			case TileChangeLayer.Base:
			case TileChangeLayer.Floor:
				if (FloorAssets.ContainsKey(tileKey))
				{
					return true;
				}
				break;
			case TileChangeLayer.Object:
			case TileChangeLayer.BrokenGrill:
				if (ObjectAssets.ContainsKey(tileKey))
				{
					return true;
				}
				break;
			case TileChangeLayer.Wall:
				if (WallAssets.ContainsKey(tileKey))
				{
					return true;
				}
				break;
			case TileChangeLayer.Window:
			case TileChangeLayer.WindowDamage:
				if (WindowAssets.ContainsKey(tileKey))
				{
					return true;
				}
				break;
		}
		return false;
	}
}