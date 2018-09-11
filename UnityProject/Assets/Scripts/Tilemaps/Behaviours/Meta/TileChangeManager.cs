using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

//To track tile changes on the server and update on the client
public class TileChangeManager : NetworkBehaviour
{
	private Tilemap floorTileMap;
	private Tilemap baseTileMap;
	private Tilemap wallTileMap;
	private Tilemap windowTileMap;
	private Tilemap objectTileMap;

	private Dictionary<string, TileBase> FloorAssets = new Dictionary<string, TileBase> ();
	private Dictionary<string, TileBase> WallAssets = new Dictionary<string, TileBase> ();
	private Dictionary<string, TileBase> WindowAssets = new Dictionary<string, TileBase> ();
	private Dictionary<string, TileBase> TableAssets = new Dictionary<string, TileBase> ();

	private TileChangeRegister ChangeRegister = new TileChangeRegister ();

	bool init = false;
	void Start ()
	{
		CacheTileMaps ();
		LoadAllTileAssets ();
	}

	[Server]
	public void NotifyPlayer (GameObject requestedBy)
	{
		Logger.LogWarning("We do not need to sync tilemap change data anymore but the system is in place " +
		"to preform database state backups", Category.TileMaps);
		
		//Example of converting all the tile change data into json which can be stored as a blob in a database:
		// var jsondata = JsonUtility.ToJson (ChangeRegister);
		// TileChangesNewClientSync.Send (gameObject, requestedBy, jsondata);
	}

	public void InitServerSync (string data)
	{
		//Unpacking the data example (and then run action change)
		var newChangeRegister = JsonUtility.FromJson<TileChangeRegister> (data);
	}

	void CacheTileMaps ()
	{
		var tilemaps = GetComponentsInChildren<Tilemap> (true);
		for (int i = 0; i < tilemaps.Length; i++)
		{
			if (tilemaps[i].name.Contains ("Floors"))
			{
				floorTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains ("Base"))
			{
				baseTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains ("Walls"))
			{
				wallTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains ("Windows"))
			{
				windowTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains ("Objects"))
			{
				objectTileMap = tilemaps[i];
			}
		}
	}

	void LoadAllTileAssets ()
	{
		TileBase[] floorTiles = Resources.LoadAll<TileBase> ("Tiles/Floors");
		for (int i = 0; i < floorTiles.Length; i++)
		{
			FloorAssets.Add (floorTiles[i].name, floorTiles[i]);
		}

		TileBase[] windowTiles = Resources.LoadAll<TileBase> ("Tiles/Windows");
		for (int i = 0; i < windowTiles.Length; i++)
		{
			WindowAssets.Add (windowTiles[i].name, windowTiles[i]);
		}

		TileBase[] wallTiles = Resources.LoadAll<TileBase> ("Tiles/Walls");
		for (int i = 0; i < wallTiles.Length; i++)
		{
			WallAssets.Add (wallTiles[i].name, wallTiles[i]);
		}

		TileBase[] tableTiles = Resources.LoadAll<TileBase> ("Tiles/Tables");
		for (int i = 0; i < tableTiles.Length; i++)
		{
			TableAssets.Add (tableTiles[i].name, tableTiles[i]);
		}
		init = true;
	}

	[Server]
	public void ChangeTile (string newTileName, Vector2Int tileCellPos, TileChangeLayer layer)
	{
		ActionChange (new TileChangeEntry
		{
			cellPosition = tileCellPos,
				tileKey = newTileName,
				layerToChange = layer
		});
	}

	[Server]
	public void RemoveTile (Vector2Int tileCellPos, TileChangeLayer layer)
	{
		ActionChange (new TileChangeEntry
		{
			cellPosition = tileCellPos,
				tileKey = "",
				layerToChange = layer
		});
	}

	private void ActionChange (TileChangeEntry changeEntry)
	{
		if (!ValidateChange (changeEntry.layerToChange, changeEntry.tileKey))
		{
			Logger.LogError ("No key found for Tile Change", Category.TileMaps);
			return;
		}

		var tileMap = GetTilemap (changeEntry.layerToChange);
		if(tileMap == null)
		{
			return;
		}
		TileBase newTile = GetTile(changeEntry.layerToChange, changeEntry.tileKey);
		tileMap.SetTile(new Vector3Int(changeEntry.cellPosition.x,
		changeEntry.cellPosition.y, 0), newTile);

		if(isServer){
			RpcActionChange(changeEntry.cellPosition, changeEntry.tileKey, changeEntry.layerToChange);
		}

		//Store the change data and later we will store it in a database (so when a server crashes it is safe or to replay station change events)
		ChangeRegister.allEntries.Add(changeEntry);
	}

	[ClientRpc]
	private void RpcActionChange(Vector2 cellPos, string tileKey, TileChangeLayer layer){
		ActionChange(new TileChangeEntry
		{
			cellPosition = Vector2Int.RoundToInt(cellPos),
			tileKey = tileKey,
			layerToChange = layer
		});
	}

	private Tilemap GetTilemap (TileChangeLayer layer)
	{
		switch (layer)
		{
			case TileChangeLayer.Base:
				return baseTileMap;
			case TileChangeLayer.Floor:
				return floorTileMap;
			case TileChangeLayer.Object:
				return objectTileMap;
			case TileChangeLayer.Wall:
				return objectTileMap;
			case TileChangeLayer.Window:
				return windowTileMap;
		}
		return null;
	}

	private TileBase GetTile (TileChangeLayer layer, string tileKey)
	{
		switch (layer)
		{
			case TileChangeLayer.Base:
			case TileChangeLayer.Floor:
				if (FloorAssets.ContainsKey (tileKey))
				{
					return FloorAssets[tileKey];
				}
				break;
			case TileChangeLayer.Object:
				if (TableAssets.ContainsKey (tileKey))
				{
					return TableAssets[tileKey];
				}
				break;
			case TileChangeLayer.Wall:
				if (WallAssets.ContainsKey (tileKey))
				{
					return WallAssets[tileKey];
				}
				break;
			case TileChangeLayer.Window:
				if (WindowAssets.ContainsKey (tileKey))
				{
					return WindowAssets[tileKey];
				}
				break;
		}
		return null;
	}

	private bool ValidateChange (TileChangeLayer layer, string tileKey)
	{
		if (string.IsNullOrEmpty (tileKey))
		{
			return true;
		}

		switch (layer)
		{
			case TileChangeLayer.Base:
			case TileChangeLayer.Floor:
				if (FloorAssets.ContainsKey (tileKey))
				{
					return true;
				}
				break;
			case TileChangeLayer.Object:
				if (TableAssets.ContainsKey (tileKey))
				{
					return true;
				}
				break;
			case TileChangeLayer.Wall:
				if (WallAssets.ContainsKey (tileKey))
				{
					return true;
				}
				break;
			case TileChangeLayer.Window:
				if (WindowAssets.ContainsKey (tileKey))
				{
					return true;
				}
				break;
		}
		return false;
	}
}

[System.Serializable]
public class TileChangeRegister
{
	public List<TileChangeEntry> allEntries = new List<TileChangeEntry> ();
}

[System.Serializable]
public class TileChangeEntry
{
	public Vector2Int cellPosition;
	///<Summary>
	/// Set tileKey to empty string to deconstruct
	///</Summary>
	public string tileKey;

	public TileChangeLayer layerToChange;
}

public enum TileChangeLayer
{
	Floor,
	Base,
	Wall,
	Window,
	Object
}