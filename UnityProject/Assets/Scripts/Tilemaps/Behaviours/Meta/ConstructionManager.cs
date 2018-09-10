using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;
using System.Linq;

//To track construction changes on the server and update on the client
public class ConstructionManager : NetworkBehaviour
{
	private Tilemap floorTileMap;
	private Tilemap baseTileMap;
	private Tilemap wallTileMap;
	private Tilemap windowTileMap;
	private Tilemap objectTileMap;

	private Dictionary<Tilemap, TileConstructEntries> ChangeRegister = new Dictionary<Tilemap, TileConstructEntries> ();

	void Start ()
	{
		CacheTileMaps ();
	}

	void CacheTileMaps ()
	{
		var tilemaps = GetComponentsInChildren<Tilemap> (true);
		for (int i = 0; i < tilemaps.Length; i++)
		{
			if (tilemaps[i].name.Contains ("Floors"))
			{
				floorTileMap = tilemaps[i];
				ChangeRegister.Add(floorTileMap, new TileConstructEntries());
			}

			if (tilemaps[i].name.Contains ("Base"))
			{
				baseTileMap = tilemaps[i];
				ChangeRegister.Add(baseTileMap, new TileConstructEntries());
			}

			if (tilemaps[i].name.Contains ("Walls"))
			{
				wallTileMap = tilemaps[i];
				ChangeRegister.Add(wallTileMap, new TileConstructEntries());
			}

			if (tilemaps[i].name.Contains ("Windows"))
			{
				windowTileMap = tilemaps[i];
				ChangeRegister.Add(windowTileMap, new TileConstructEntries());
			}

			if (tilemaps[i].name.Contains ("Objects"))
			{
				objectTileMap = tilemaps[i];
				ChangeRegister.Add(objectTileMap, new TileConstructEntries());
			}
		}
	}
}

[System.Serializable]
public class TileConstructEntries
{
	List<TileConstructEntry> entries = new List<TileConstructEntry> ();
}

[System.Serializable]
public class TileConstructEntry
{
	public Vector2Int gridPosition;
	///<Summary>
	/// Set tile to null if this is a tile removal
	///</Summary>
	public Tile tile;
}