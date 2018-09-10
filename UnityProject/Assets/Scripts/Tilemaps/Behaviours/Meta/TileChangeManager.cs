using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;
using System.Linq;

//To track construction and damage changes on the server and update on the client
public class TileChangeManager : NetworkBehaviour
{
	private Tilemap floorTileMap;
	private Tilemap baseTileMap;
	private Tilemap wallTileMap;
	private Tilemap windowTileMap;
	private Tilemap objectTileMap;

	private Dictionary<string, TileBase> FloorAssets = new Dictionary<string, TileBase>();
	private Dictionary<string, TileBase> WallAssets = new Dictionary<string, TileBase>();
	private Dictionary<string, TileBase> WindowAssets = new Dictionary<string, TileBase>();
	private Dictionary<string, TileBase> TableAssets = new Dictionary<string, TileBase>();

	private Dictionary<Tilemap, TileChangeEntries> ChangeRegister = new Dictionary<Tilemap, TileChangeEntries>();

	void Start ()
	{
		CacheTileMaps ();
		LoadAllTileAssets();
	}

	void CacheTileMaps ()
	{
		var tilemaps = GetComponentsInChildren<Tilemap> (true);
		for (int i = 0; i < tilemaps.Length; i++)
		{
			if (tilemaps[i].name.Contains ("Floors"))
			{
				floorTileMap = tilemaps[i];
				ChangeRegister.Add(floorTileMap, new TileChangeEntries());
			}

			if (tilemaps[i].name.Contains ("Base"))
			{
				baseTileMap = tilemaps[i];
				ChangeRegister.Add(baseTileMap, new TileChangeEntries());
			}

			if (tilemaps[i].name.Contains ("Walls"))
			{
				wallTileMap = tilemaps[i];
				ChangeRegister.Add(wallTileMap, new TileChangeEntries());
			}

			if (tilemaps[i].name.Contains ("Windows"))
			{
				windowTileMap = tilemaps[i];
				ChangeRegister.Add(windowTileMap, new TileChangeEntries());
			}

			if (tilemaps[i].name.Contains ("Objects"))
			{
				objectTileMap = tilemaps[i];
				ChangeRegister.Add(objectTileMap, new TileChangeEntries());
			}
		}
	}

	void LoadAllTileAssets()
	{
		TileBase[] floorTiles = Resources.LoadAll<TileBase>("Tiles/Floors");
		for(int i = 0; i < floorTiles.Length; i++)
		{
			FloorAssets.Add(floorTiles[i].name, floorTiles[i]);
		}

		TileBase[] windowTiles = Resources.LoadAll<TileBase>("Tiles/Windows");
		for (int i = 0; i < windowTiles.Length; i++)
		{
			WindowAssets.Add(windowTiles[i].name, windowTiles[i]);
		}

		TileBase[] wallTiles = Resources.LoadAll<TileBase>("Tiles/Walls");
		for (int i = 0; i < wallTiles.Length; i++)
		{
			WallAssets.Add(wallTiles[i].name, wallTiles[i]);
		}

		TileBase[] tableTiles = Resources.LoadAll<TileBase>("Tiles/Tables");
		for (int i = 0; i < tableTiles.Length; i++)
		{
			TableAssets.Add(tableTiles[i].name, tableTiles[i]);
		}
	}
}

[System.Serializable]
public class TileChangeEntries
{
	List<TileChangeEntry> entries = new List<TileChangeEntry> ();
}

[System.Serializable]
public class TileChangeEntry
{
	public Vector2Int gridPosition;
	///<Summary>
	/// Set tile to null if this is a tile removal
	///</Summary>
	public Tile tile;
}