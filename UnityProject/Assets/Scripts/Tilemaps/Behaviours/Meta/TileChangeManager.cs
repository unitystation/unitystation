using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

//To track construction and damage changes on the server and update on the client
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

	private TileChangeRegister ChangeRegister = new TileChangeRegister();

	bool init = false;
	void Start ()
	{
		CacheTileMaps ();
		LoadAllTileAssets ();
	}

	[Server]
	public void NotifyPlayer(GameObject requestedBy){
		Debug.Log ("Request all updates: " + requestedBy.name);
		var jsondata = JsonUtility.ToJson(ChangeRegister);
		TileChangesNewClientSync.Send(gameObject, requestedBy, jsondata);
	}

	public void InitServerSync(string data){
		Debug.Log("DATA " + data);
		var newChangeRegister = JsonUtility.FromJson<TileChangeRegister>(data);
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
}

[System.Serializable]
public class TileChangeRegister{
	public List<TileChangeEntry> floorEntries = new List<TileChangeEntry> ();
	public List<TileChangeEntry> baseEntries = new List<TileChangeEntry> ();
	public List<TileChangeEntry> wallEntries = new List<TileChangeEntry> ();
	public List<TileChangeEntry> windowEntries = new List<TileChangeEntry> ();
	public List<TileChangeEntry> objectEntries = new List<TileChangeEntry> ();
}

[System.Serializable]
public class TileChangeEntry
{
	public Vector2Int gridPosition;
	///<Summary>
	/// Set tileKey to empty string to deconstruct
	///</Summary>
	public string tileKey;
}